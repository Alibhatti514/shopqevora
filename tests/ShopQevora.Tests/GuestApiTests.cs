using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using ShopQevora.Api;
using Xunit;
namespace ShopQevora.Tests;
public sealed class GuestApiFactory:WebApplicationFactory<Program>
{
    readonly string database=Guid.NewGuid().ToString();
    readonly string signingKey=Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_,configuration)=>configuration.AddInMemoryCollection(new Dictionary<string,string?>{["ConnectionStrings:Shop"]="unused-test-connection",["Jwt:Key"]=signingKey}));
        builder.ConfigureTestServices(services=>{
            services.RemoveAll<ShopDb>();
            services.AddScoped(_=>new ShopDb(new DbContextOptionsBuilder<ShopDb>().UseInMemoryDatabase(database).ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options));
        });
    }
    public string Token(string user,string issuer="ShopQevora",string audience="ShopQevoraClient")=>new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer,audience,[new Claim(ClaimTypes.NameIdentifier,user)],expires:DateTime.UtcNow.AddMinutes(5),signingCredentials:new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),SecurityAlgorithms.HmacSha256)));
    public async Task Seed()
    {
        using var scope=Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ShopDb>();db.Products.Add(new(){ProductID=1,Name="Cup",Price=20,Category="Home"});db.Orders.AddRange(new Order{UserId="alice",Total=12},new Order{UserId="bob",Total=15});await db.SaveChangesAsync();
    }
}
public class GuestApiTests
{
    [Fact]public async Task AnonymousGuestOrderWorksButPrivateOrdersRemainProtected()
    {
        await using var app=new GuestApiFactory();using var client=app.CreateClient();await app.Seed();var response=await client.PostAsJsonAsync("/api/guest-orders",GuestOrderTests.Valid());Assert.Equal(HttpStatusCode.OK,response.StatusCode);var receipt=await response.Content.ReadFromJsonAsync<GuestReceipt>();Assert.Equal(40,receipt!.Total);
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/orders")).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/cart")).StatusCode);
        var lookup=await client.GetAsync("/api/guest-orders/"+receipt.Reference);Assert.Equal(HttpStatusCode.NotFound,lookup.StatusCode);
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",app.Token("alice"));var orders=await client.GetFromJsonAsync<List<Order>>("/api/orders");Assert.Single(orders!);Assert.Equal("alice",orders![0].UserId);Assert.DoesNotContain(receipt.Contact.Email,await client.GetStringAsync("/api/orders"));
    }
    [Fact]public async Task InvalidGuestRequestReturns400WithoutCreatingOrder()
    {
        await using var app=new GuestApiFactory();using var client=app.CreateClient();await app.Seed();var request=GuestOrderTests.Valid();request.Contact=null;Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/guest-orders",request)).StatusCode);
    }
    [Fact]public async Task MergeIsProtectedIdempotentAndKeepsLargerQuantity()
    {
        await using var app=new GuestApiFactory();using var client=app.CreateClient();await app.Seed();var lines=new[]{new GuestLine(1,3)};Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsJsonAsync("/api/cart/merge",lines)).StatusCode);
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",app.Token("alice"));Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsJsonAsync("/api/cart/merge",lines)).StatusCode);await client.PostAsJsonAsync("/api/cart/merge",lines);await client.PostAsJsonAsync("/api/cart/merge",new[]{new GuestLine(1,2)});var cart=await client.GetFromJsonAsync<List<CartItem>>("/api/cart");Assert.Single(cart!);Assert.Equal(3,cart![0].Quantity);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/orders",new{})).StatusCode);Assert.Empty((await client.GetFromJsonAsync<List<CartItem>>("/api/cart"))!);
    }
    [Fact]public async Task ClientSuppliedPricesAreIgnoredAndReusedKeyConflictsSafely()
    {
        await using var app=new GuestApiFactory();using var client=app.CreateClient();await app.Seed();var r=GuestOrderTests.Valid();
        var response=await client.PostAsJsonAsync("/api/guest-orders",new{r.CheckoutKey,r.Contact,Total=0.01m,Lines=new[]{new{ProductID=1,Quantity=2,UnitPrice=0.01m}}});
        Assert.Equal(40,(await response.Content.ReadFromJsonAsync<GuestReceipt>())!.Total);r.Lines=[new(1,3)];Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/guest-orders",r)).StatusCode);
    }
    [Fact]public async Task GuestEndpointRateLimitsRepeatedRequests()
    {
        await using var app=new GuestApiFactory();using var client=app.CreateClient();HttpResponseMessage? response=null;
        for(var i=0;i<11;i++)response=await client.PostAsJsonAsync("/api/guest-orders",new{});
        Assert.Equal(HttpStatusCode.TooManyRequests,response!.StatusCode);
    }
    [Fact]public async Task ExistingSignedSessionsStillAuthorizeOnlyTheirOwnHistory()
    {
        await using var app=new GuestApiFactory();using var client=app.CreateClient();await app.Seed();client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",app.Token("alice","ShopEase","ShopEaseClient"));
        var orders=await client.GetFromJsonAsync<List<Order>>("/api/orders");Assert.Single(orders!);Assert.Equal("alice",orders![0].UserId);
    }

    [Fact]public async Task RegistrationLoginCartCheckoutAndOwnHistoryRemainFunctional()
    {
        await using var app=new GuestApiFactory();using var client=app.CreateClient();await app.Seed();var credentials=new{Email="member@example.test",Password="A1!"+Guid.NewGuid().ToString("N")};
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/auth/register",credentials)).StatusCode);
        var login=await client.PostAsJsonAsync("/api/auth/login",credentials);Assert.Equal(HttpStatusCode.OK,login.StatusCode);
        using var body=System.Text.Json.JsonDocument.Parse(await login.Content.ReadAsStringAsync());client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",body.RootElement.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsync("/api/cart/1",null)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await client.PostAsync("/api/orders",null)).StatusCode);
        var history=await client.GetFromJsonAsync<List<Order>>("/api/orders");Assert.Single(history!);Assert.Equal(20,history![0].Total);
    }

}
