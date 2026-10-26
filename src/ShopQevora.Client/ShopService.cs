using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ShopQevora.Contracts;
using Microsoft.JSInterop;
namespace ShopQevora.Client;

public class ShopService(HttpClient http,IJSRuntime js)
{
    public event Action? Changed;
    public event Action? CatalogResetRequested;
    public void BrowseAll()=>CatalogResetRequested?.Invoke();
    public string? Email {get;private set;}
    public bool LoggedIn=>!string.IsNullOrEmpty(token);
    public int CartCount {get;private set;}
    public string Message {get;private set;}="";
    public GuestReceipt? GuestConfirmation {get;private set;}
    public bool HasPendingGuestCart=>LoggedIn&&guest.State.Count>0;
    readonly GuestCartStore guest=new(js);
    readonly SemaphoreSlim initLock=new(1,1);
    string? token;bool initialized;
    List<Product>? catalog;
    public async Task Initialize()
    {
        await initLock.WaitAsync();
        try {
            if(initialized)return;
            token=await js.InvokeAsync<string?>("sessionStorage.getItem","shopqevora_token");
            Email=await js.InvokeAsync<string?>("sessionStorage.getItem","shopqevora_email");
            // One-time compatibility for pre-rebrand sessions. No passwords are stored.
            if(token is null){token=await js.InvokeAsync<string?>("sessionStorage.getItem","shopease_token");Email=await js.InvokeAsync<string?>("sessionStorage.getItem","shopease_email");if(token is not null){await SaveSession();await js.InvokeVoidAsync("sessionStorage.removeItem","shopease_token");await js.InvokeVoidAsync("sessionStorage.removeItem","shopease_email");}}
            await js.InvokeVoidAsync("sessionStorage.removeItem","shopease_token");
            await js.InvokeVoidAsync("sessionStorage.removeItem","shopease_email");
            await guest.Load();initialized=true;
        }
        finally{initLock.Release();}
        await RefreshCount();
    }
    async Task SaveSession(){await js.InvokeVoidAsync("sessionStorage.setItem","shopqevora_token",token);await js.InvokeVoidAsync("sessionStorage.setItem","shopqevora_email",Email);}
    HttpRequestMessage Request(HttpMethod method,string url){var request=new HttpRequestMessage(method,url);if(token is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);return request;}
    async Task RefreshCount(){try{CartCount=LoggedIn?(await AuthCart()).Sum(x=>x.Quantity):guest.State.Count;}catch{CartCount=0;}Changed?.Invoke();}
    async Task<List<CartItem>> AuthCart(){using var req=Request(HttpMethod.Get,"api/cart");using var response=await http.SendAsync(req);response.EnsureSuccessStatusCode();return await response.Content.ReadFromJsonAsync<List<CartItem>>()??[];}
    public async Task<List<Product>> Products(string search="",string category="")
    {
        var products=await http.GetFromJsonAsync<List<Product>>($"api/products?search={Uri.EscapeDataString(search)}&category={Uri.EscapeDataString(category)}")??[];
        if(search==""&&category=="")catalog=products;return products;
    }
    public async Task<string?> Register(string email,string password){using var response=await http.PostAsJsonAsync("api/auth/register",new{email,password});if(response.IsSuccessStatusCode)return null;return await Error(response,"Could not create your account.");}
    public async Task<string?> Login(string email,string password)
    {
        await Initialize();using var response=await http.PostAsJsonAsync("api/auth/login",new{email,password});if(!response.IsSuccessStatusCode)return "Incorrect email or password.";
        GuestConfirmation=null;var result=await response.Content.ReadFromJsonAsync<LoginResult>();token=result?.Token;Email=result?.Email;await SaveSession();
        try{await MergeGuestCart();}catch{Message="Signed in. Your local guest bag is preserved. Retry merging it below.";}
        await RefreshCount();return null;
    }
    public async Task MergeGuestCart()
    {
        if(!LoggedIn||guest.State.Count==0)return;
        using var req=Request(HttpMethod.Post,"api/cart/merge");req.Content=JsonContent.Create(guest.State.Lines);
        using var response=await http.SendAsync(req);if(!response.IsSuccessStatusCode)throw new InvalidOperationException(await Error(response,"Could not merge your guest bag."));
        await guest.Clear();Message="Guest bag merged. Shared products keep the larger quantity, up to 99.";await RefreshCount();
    }
    public async Task Logout(){GuestConfirmation=null;token=null;Email=null;Message="";await js.InvokeVoidAsync("sessionStorage.removeItem","shopqevora_token");await js.InvokeVoidAsync("sessionStorage.removeItem","shopqevora_email");await js.InvokeVoidAsync("sessionStorage.removeItem","shopease_token");await js.InvokeVoidAsync("sessionStorage.removeItem","shopease_email");await RefreshCount();}
    public async Task<List<CartItem>> Cart()
    {
        await Initialize();if(LoggedIn)return await AuthCart();
        var products=catalog??await Products();
        return guest.State.Lines.Select(l=>new CartItem{ProductID=l.ProductID,Quantity=l.Quantity,Product=products.FirstOrDefault(p=>p.ProductID==l.ProductID)??new Product{ProductID=l.ProductID,Name=$"Unavailable item #{l.ProductID}",Description="Remove this unavailable item before checking out."}}).ToList();
    }
    public async Task Add(int id){await Initialize();if(LoggedIn){using var req=Request(HttpMethod.Post,$"api/cart/{id}");using var r=await http.SendAsync(req);r.EnsureSuccessStatusCode();}else{guest.State.Add(id);await guest.Save();}await RefreshCount();}
    public async Task Remove(int id){await Initialize();if(LoggedIn){using var req=Request(HttpMethod.Delete,$"api/cart/{id}");using var r=await http.SendAsync(req);r.EnsureSuccessStatusCode();}else{guest.State.Remove(id);await guest.Save();}await RefreshCount();}
    public async Task SetQuantity(int id,int quantity){if(quantity is <1 or >99)throw new ArgumentOutOfRangeException(nameof(quantity));await Initialize();if(LoggedIn){using var req=Request(HttpMethod.Put,$"api/cart/{id}");req.Content=JsonContent.Create(new{quantity});using var r=await http.SendAsync(req);r.EnsureSuccessStatusCode();}else{guest.State.Set(id,quantity);await guest.Save();}await RefreshCount();}
    public async Task<GuestReceipt> GuestCheckout(GuestContact contact)
    {
        await Initialize();if(LoggedIn)throw new InvalidOperationException("Use your account checkout while signed in.");
        using var response=await http.PostAsJsonAsync("api/guest-orders",new{CheckoutKey=guest.State.CheckoutKey,Contact=contact,Lines=guest.State.Lines});
        if(!response.IsSuccessStatusCode)throw new InvalidOperationException(await Error(response,"Could not place your demo order. Your bag is preserved."));
        var receipt=await response.Content.ReadFromJsonAsync<GuestReceipt>()??throw new InvalidOperationException("The order response was incomplete. Your bag is preserved.");
        if(receipt.Reference is null || !receipt.Reference.StartsWith("GQ-",StringComparison.Ordinal) || receipt.Reference.Length!=35 || receipt.Contact is null || receipt.Lines is null || receipt.Lines.Count==0 || receipt.Lines.Any(l=>l is null || l.Quantity is <1 or >99 || l.UnitPrice<=0) || receipt.Total!=receipt.Lines.Sum(l=>l.UnitPrice*l.Quantity))
            throw new InvalidOperationException("The order response was incomplete. Your bag is preserved; retry with the same details.");
        GuestConfirmation=receipt;
        // Clear only after a complete successful response. A failed request never touches the bag.
        try{await guest.Clear();}catch{Message="Your order is confirmed, but browser storage could not be cleared. Keep your reference and check your bag before ordering again.";}
        await RefreshCount();return receipt;
    }
    public async Task<int> Checkout(){using var req=Request(HttpMethod.Post,"api/orders");using var response=await http.SendAsync(req);response.EnsureSuccessStatusCode();var result=await response.Content.ReadFromJsonAsync<CheckoutResult>();await RefreshCount();return result?.Id??0;}
    public async Task<List<Order>> Orders(){using var req=Request(HttpMethod.Get,"api/orders");using var response=await http.SendAsync(req);response.EnsureSuccessStatusCode();return await response.Content.ReadFromJsonAsync<List<Order>>()??[];}
    public async Task<ContactChallenge> ContactChallenge(string? key=null)=>await http.GetFromJsonAsync<ContactChallenge>("api/contact/challenge?submissionKey="+Uri.EscapeDataString(key??""))??throw new InvalidOperationException("Could not prepare the contact form.");
    public async Task<ContactReceipt> SendInquiry(ContactRequest input)
    {
        using var request=Request(HttpMethod.Post,"api/contact/inquiries");request.Content=JsonContent.Create(input);
        using var response=await http.SendAsync(request);
        if(!response.IsSuccessStatusCode)throw new InvalidOperationException(await Error(response,"Could not save your inquiry. Your message is preserved; please try again."));
        var receipt=await response.Content.ReadFromJsonAsync<ContactReceipt>();
        if(receipt is null||!System.Text.RegularExpressions.Regex.IsMatch(receipt.Reference??"","^CQ-[A-F0-9]{32}$"))throw new InvalidOperationException("The confirmation was incomplete. Your message is preserved; retry with the same details.");
        return receipt;
    }
    public async Task<CustomerProfile> Profile(){await Initialize();using var request=Request(HttpMethod.Get,"api/account/profile");using var response=await http.SendAsync(request);if(!response.IsSuccessStatusCode)throw new InvalidOperationException(await Error(response,"Could not load your profile. Sign in again or retry."));return (await response.Content.ReadFromJsonAsync<CustomerProfile>())!;}
    public async Task<List<DialingCode>> DialingCodes(){using var request=Request(HttpMethod.Get,"api/account/dialing-codes");using var response=await http.SendAsync(request);response.EnsureSuccessStatusCode();return await response.Content.ReadFromJsonAsync<List<DialingCode>>()??[];}
    public Task<string?> SaveProfile(CustomerProfile p)=>AccountAction("profile",new{p.FullName,p.DisplayName,p.Phone,p.PhoneRegion,p.Address,p.City,p.Region,p.PostalCode,p.Country},HttpMethod.Put);
    public async Task<string?> AccountAction(string path,object? body=null,HttpMethod? method=null){using var request=Request(method??HttpMethod.Post,"api/account/"+path);if(body is not null)request.Content=JsonContent.Create(body);using var response=await http.SendAsync(request);return response.IsSuccessStatusCode?null:await Error(response,"Could not save your changes. Please retry.");}
    static async Task<string> Error(HttpResponseMessage response,string fallback)
    {
        var body=await response.Content.ReadAsStringAsync();
        try{using var document=JsonDocument.Parse(body);var root=document.RootElement;if(root.ValueKind==JsonValueKind.Array)return string.Join(" ",root.EnumerateArray().Select(x=>x.GetString()));if(root.ValueKind==JsonValueKind.String)return root.GetString()??fallback;foreach(var key in new[]{"message","detail","title"})if(root.TryGetProperty(key,out var text))return text.GetString()??fallback;}catch(JsonException){}
        return fallback;
    }
    record LoginResult(string Token,string Email);record CheckoutResult(int Id,decimal Total);
}
