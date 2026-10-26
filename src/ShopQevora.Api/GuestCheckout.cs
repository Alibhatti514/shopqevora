using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace ShopQevora.Api;

public sealed class GuestContact
{
    [Required, StringLength(120, MinimumLength=2)] public string FullName {get;set;}="";
    [Required, EmailAddress, StringLength(254)] public string Email {get;set;}="";
    [Required, StringLength(30), RegularExpression(@"^\+?[0-9][0-9 ()-]{6,28}[0-9]$")] public string Phone {get;set;}="";
    [Required, StringLength(500, MinimumLength=5)] public string Address {get;set;}="";
    [Required, StringLength(80)] public string City {get;set;}="";
    [Required, StringLength(80)] public string Region {get;set;}="";
    [StringLength(20)] public string PostalCode {get;set;}="";
    [Required] public string Country {get;set;}="PK";
    public void Normalize() { FullName=FullName?.Trim()??""; Email=Email?.Trim().ToLowerInvariant()??""; Phone=Phone?.Trim()??""; Address=Address?.Trim()??""; City=City?.Trim()??""; Region=Region?.Trim()??""; PostalCode=PostalCode?.Trim()??""; Country=Country?.Trim().ToUpperInvariant()??""; }
}
public sealed record GuestLine(int ProductID,int Quantity);
public sealed class GuestOrderRequest
{
    public string CheckoutKey {get;set;}="";
    public GuestContact? Contact {get;set;}
    public List<GuestLine>? Lines {get;set;}
    public IReadOnlyList<string> Validate()
    {
        Contact?.Normalize();
        var errors=new List<string>();
        if(!Guid.TryParseExact(CheckoutKey,"N",out var key)||key==Guid.Empty)errors.Add("A valid private checkout key is required.");else CheckoutKey=key.ToString("N");
        if(Contact is null)errors.Add("Contact and address details are required.");
        else { var results=new List<ValidationResult>(); Validator.TryValidateObject(Contact,new ValidationContext(Contact),results,true);errors.AddRange(results.Select(x=>x.ErrorMessage??"Invalid contact details."));if(Contact.Country!="PK")errors.Add("Only Pakistan demo addresses are supported."); }
        errors.AddRange(GuestOrderService.ValidateLines(Lines));
        return errors;
    }
}
public sealed class GuestCheckout
{
    public int OrderId {get;set;}
    public Order Order {get;set;}=null!;
    [MaxLength(64)] public string Reference {get;set;}="";
    [MaxLength(64)] public string CheckoutKeyHash {get;set;}="";
    [MaxLength(64)] public string RequestHash {get;set;}="";
    public string ContactJson {get;set;}="";
}
public sealed record GuestReceiptLine(string ProductName,decimal UnitPrice,int Quantity);
public sealed record GuestReceipt(string Reference,decimal Total,GuestContact Contact,List<GuestReceiptLine> Lines,string PaymentStatus="Demo — no payment taken",string Status="Demo order confirmed; no fulfillment");
public sealed class CheckoutValidationException(string message):Exception(message);
public sealed class CheckoutConflictException():Exception("An order already exists for this bag key. Retry with the same contact details, or change the bag to start a separate order. No additional order was created.");

public sealed class GuestOrderService(ShopDb db)
{
    public static IEnumerable<string> ValidateLines(IReadOnlyList<GuestLine>? lines)
    {
        if(lines is null || lines.Count is <1 or >50) { yield return "Choose between 1 and 50 distinct products."; yield break; }
        if(lines.Any(l=>l is null || l.ProductID<=0 || l.Quantity is <1 or >99))yield return "Every product needs a valid ID and a quantity from 1 to 99.";
        if(lines.Where(l=>l is not null).Select(l=>l.ProductID).Distinct().Count()!=lines.Count)yield return "Duplicate product lines are not allowed.";
    }
    public async Task<GuestReceipt> Place(GuestOrderRequest request)
    {
        var errors=request.Validate();if(errors.Count>0)throw new CheckoutValidationException(string.Join(" ",errors));
        var keyHash=Hash(request.CheckoutKey);
        var requestHash=Hash(JsonSerializer.Serialize(new { Contact=request.Contact, Lines=request.Lines!.OrderBy(l=>l.ProductID) }));
        var existing=await db.GuestCheckouts.Include(x=>x.Order).ThenInclude(x=>x.Lines).SingleOrDefaultAsync(x=>x.CheckoutKeyHash==keyHash);
        if(existing is not null)return Replay(existing,requestHash);
        var ids=request.Lines!.Select(l=>l.ProductID).ToArray();
        var products=await db.Products.AsNoTracking().Where(p=>ids.Contains(p.ProductID)).ToDictionaryAsync(p=>p.ProductID);
        if(products.Count!=ids.Length || products.Values.Any(p=>p.Price is <=0 or >1000000))throw new CheckoutValidationException("One or more selected products are unavailable. Review your bag.");
        var lines=request.Lines!.Select(l=>new OrderLine { ProductName=products[l.ProductID].Name, UnitPrice=products[l.ProductID].Price, Quantity=l.Quantity }).ToList();
        var reference="GQ-"+Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var order=new Order { UserId="guest:"+reference, Lines=lines, Total=lines.Sum(l=>l.UnitPrice*l.Quantity) };
        var checkout=new GuestCheckout { Order=order,Reference=reference,CheckoutKeyHash=keyHash,RequestHash=requestHash,ContactJson=JsonSerializer.Serialize(request.Contact) };
        db.GuestCheckouts.Add(checkout);
        // One SaveChanges persists order, lines and companion row atomically in MySQL.
        // Unique retry-key index prevents duplicate orders across concurrent retries.
        try { await db.SaveChangesAsync(); }
        catch(DbUpdateException) {
            db.ChangeTracker.Clear();
            var replay=await db.GuestCheckouts.Include(x=>x.Order).ThenInclude(x=>x.Lines).SingleOrDefaultAsync(x=>x.CheckoutKeyHash==keyHash);
            if(replay is null)throw;return Replay(replay,requestHash);
        }
        return Receipt(checkout);
    }
    private static GuestReceipt Replay(GuestCheckout checkout,string hash) { if(checkout.RequestHash!=hash)throw new CheckoutConflictException();return Receipt(checkout); }
    private static GuestReceipt Receipt(GuestCheckout checkout)=>new(checkout.Reference,checkout.Order.Total,JsonSerializer.Deserialize<GuestContact>(checkout.ContactJson)!,checkout.Order.Lines.Select(l=>new GuestReceiptLine(l.ProductName,l.UnitPrice,l.Quantity)).ToList());
    private static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
