using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ShopQevora.Contracts;
namespace ShopQevora.Api;

public sealed class ContactInquiry
{
    public int Id { get; set; }
    [MaxLength(35)] public string Reference { get; set; } = "";
    [MaxLength(64)] public string SubmissionKeyHash { get; set; } = "";
    [MaxLength(64)] public string RequestHash { get; set; } = "";
    [MaxLength(120)] public string FullName { get; set; } = "";
    [MaxLength(254)] public string Email { get; set; } = "";
    [MaxLength(32)] public string? Phone { get; set; }
    [MaxLength(32)] public string Category { get; set; } = "";
    [MaxLength(64)] public string? OrderReference { get; set; }
    [MaxLength(4000)] public string Message { get; set; } = "";
    // Server-derived attribution only. No FK or changes to existing Identity records.
    [MaxLength(255)] public string? UserId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ContactFormTokens(IDataProtectionProvider provider, TimeProvider clock)
{
    readonly IDataProtector protector=provider.CreateProtector("ShopQevora.ContactForm.v1");
    public ContactChallenge Issue(string? existingKey=null)
    {
        var key=Guid.TryParseExact(existingKey,"N",out var existing)&&existing!=Guid.Empty?existing.ToString("N"):Guid.NewGuid().ToString("N");
        return new(key, protector.Protect(key+":"+clock.GetUtcNow().ToUnixTimeSeconds()));
    }
    public bool Valid(string key, string token)
    {
        if(!Guid.TryParseExact(key,"N",out var id)||id==Guid.Empty||string.IsNullOrEmpty(token)||token.Length>1024)return false;
        try
        {
            var parts=protector.Unprotect(token).Split(':');
            if(parts.Length!=2||parts[0]!=key||!long.TryParse(parts[1],out var issued))return false;
            var age=clock.GetUtcNow().ToUnixTimeSeconds()-issued;
            return age>=0&&age<=1200;
        }
        catch(CryptographicException){return false;}
    }
}

public sealed class ContactSubmissionException(string message):Exception(message);
public sealed class ContactSubmissionConflict():Exception("This form session already saved a different inquiry. Start a new inquiry before sending different details.");
public sealed class ContactInquiryService(ShopDb db,ContactFormTokens tokens)
{
    public async Task<(ContactReceipt Receipt,bool Created)> Submit(ContactRequest request,string? userId,CancellationToken cancellation=default)
    {
        request.Normalize();
        var validation=new List<ValidationResult>();
        if(!Validator.TryValidateObject(request,new ValidationContext(request),validation,true))
            throw new ContactSubmissionException(string.Join(" ",validation.Select(v=>v.ErrorMessage)));
        if(!string.IsNullOrEmpty(request.Website))throw new ContactSubmissionException("The submission could not be accepted. Please reload the form and try again.");
        if(!tokens.Valid(request.SubmissionKey,request.FormToken))throw new ContactSubmissionException("Your form session has expired or is invalid. Refresh the form session and try again; your message is preserved.");
        var phone=ProfileEndpoints.NormalizePhone(request.Phone,"ZZ");
        if(phone is null)throw new ContactSubmissionException("Enter a valid international phone number with its country code, or leave phone blank.");
        var keyHash=Hash(request.SubmissionKey);
        var requestHash=Hash(JsonSerializer.Serialize(new{request.FullName,request.Email,Phone=phone,request.Category,request.OrderReference,request.Message,UserId=userId}));
        var existing=await db.ContactInquiries.AsNoTracking().SingleOrDefaultAsync(x=>x.SubmissionKeyHash==keyHash,cancellation);
        if(existing is not null)return Replay(existing,requestHash);
        var inquiry=new ContactInquiry{Reference="CQ-"+Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),SubmissionKeyHash=keyHash,RequestHash=requestHash,FullName=request.FullName,Email=request.Email,Phone=string.IsNullOrEmpty(phone)?null:phone,Category=request.Category,OrderReference=request.OrderReference,Message=request.Message,UserId=userId};
        db.ContactInquiries.Add(inquiry);
        try{await db.SaveChangesAsync(cancellation);}
        catch(DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var replay=await db.ContactInquiries.AsNoTracking().SingleOrDefaultAsync(x=>x.SubmissionKeyHash==keyHash,cancellation);
            if(replay is null)throw;
            return Replay(replay,requestHash);
        }
        return (new(inquiry.Reference,inquiry.CreatedUtc),true);
    }
    static (ContactReceipt,bool) Replay(ContactInquiry inquiry,string requestHash)
    {
        if(inquiry.RequestHash!=requestHash)throw new ContactSubmissionConflict();
        return (new(inquiry.Reference,inquiry.CreatedUtc),false);
    }
    static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public static class ContactEndpoints
{
    public static void MapContact(this WebApplication app)
    {
        app.MapGet("/api/contact/challenge",(ContactFormTokens tokens,HttpContext context,string? submissionKey)=>
        {
            context.Response.Headers.CacheControl="no-store";
            return Results.Ok(tokens.Issue(submissionKey));
        }).AllowAnonymous().RequireRateLimiting("contact-challenge");
        app.MapPost("/api/contact/inquiries",async(ContactRequest input,ContactInquiryService service,ShopDb db,HttpContext context)=>
        {
            context.Response.Headers.CacheControl="no-store";
            if(!await SchemaExists(db))return Results.Problem("Contact submissions are temporarily unavailable. The operator must apply the documented contact schema upgrade.",statusCode:503);
            var userId=context.User.Identity?.IsAuthenticated==true?context.User.FindFirstValue(ClaimTypes.NameIdentifier):null;
            try
            {
                var result=await service.Submit(input,userId,context.RequestAborted);
                return Results.Json(result.Receipt,statusCode:result.Created?201:200);
            }
            catch(ContactSubmissionException e){return Results.BadRequest(new{message=e.Message});}
            catch(ContactSubmissionConflict e){return Results.Conflict(new{message=e.Message});}
        }).AllowAnonymous().RequireRateLimiting("contact-submit");
        // Deliberately no public, customer or anonymous inbox/lookup endpoints.
    }
    public static async Task<bool> SchemaExists(ShopDb db)
    {
        if(!db.Database.IsRelational())return true;
        var connection=db.Database.GetDbConnection();var close=connection.State!=ConnectionState.Open;
        if(close)await connection.OpenAsync();
        try{using var command=connection.CreateCommand();command.CommandText="SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='ContactInquiries'";return Convert.ToInt32(await command.ExecuteScalarAsync())==1;}
        finally{if(close)await connection.CloseAsync();}
    }
}
