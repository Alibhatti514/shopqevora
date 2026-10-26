using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using ShopQevora.Client;
using Xunit;
namespace ShopQevora.Tests;

public class GuestCartTests
{
    public sealed class BrowserMemory : IJSRuntime
    {
        public Dictionary<string,string> Storage {get;}=new();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier,object?[]? args)=>InvokeAsync<TValue>(identifier,CancellationToken.None,args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier,CancellationToken cancellationToken,object?[]? args)
        {
            var key=identifier.Split('.')[0]+":"+args![0];object? result=null;
            if(identifier.EndsWith("getItem"))Storage.TryGetValue(key,out var value);else if(identifier.EndsWith("setItem"))Storage[key]=args[1]?.ToString()??"";else if(identifier.EndsWith("removeItem"))Storage.Remove(key);
            if(identifier.EndsWith("getItem"))result=Storage.GetValueOrDefault(key);
            return ValueTask.FromResult(result is null?default!:(TValue)result);
        }
    }
    sealed class Handler(Func<HttpRequestMessage,Task<HttpResponseMessage>> handle):HttpMessageHandler {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>handle(request);}
    static HttpClient Http(Func<HttpRequestMessage,Task<HttpResponseMessage>>? send=null)=>new(new Handler(send??(_=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new[]{new Product{ProductID=1,Name="Cup",Price=12.50m,Category="Home"},new Product{ProductID=2,Name="Bag",Price=25,Category="Lifestyle"}})})))){BaseAddress=new Uri("http://localhost/")};
    [Fact]public async Task GuestBagPersistsQuantityRemovalAndRefresh()
    {
        var browser=new BrowserMemory();var shop=new ShopService(Http(),browser);
        await shop.Add(1);await shop.Add(1);await shop.Add(2);await shop.SetQuantity(1,4);await shop.Remove(2);
        var refreshed=new ShopService(Http(),browser);var cart=await refreshed.Cart();
        Assert.Single(cart);Assert.Equal(4,cart[0].Quantity);Assert.Equal(50m,cart.Sum(i=>i.Product.Price*i.Quantity));Assert.Equal(4,refreshed.CartCount);
        Assert.DoesNotContain("Price",browser.Storage.Values.Single());
    }
    [Theory][InlineData(0)][InlineData(100)][InlineData(-1)]public async Task InvalidGuestQuantityIsRejectedAndBagSurvives(int quantity)
    {
        var shop=new ShopService(Http(),new BrowserMemory());await shop.Add(1);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()=>shop.SetQuantity(1,quantity));Assert.Equal(1,(await shop.Cart()).Single().Quantity);
    }
    [Fact]public async Task SuccessfulCheckoutClearsOnlyAfterResponseAndDoesNotStorePersonalDetails()
    {
        var browser=new BrowserMemory();var contact=new GuestContact{FullName="Demo Buyer",Email="buyer@example.test",Phone="03001234567",Address="Demo Street 12",City="Karachi",Region="Sindh"};
        var http=Http(async request=>{
            var body=await request.Content!.ReadAsStringAsync();Assert.DoesNotContain("UnitPrice",body);Assert.DoesNotContain("Total",body);Assert.True(JsonDocument.Parse(body).RootElement.TryGetProperty("contact",out _));
            Assert.Contains("localStorage:shopqevora_guest_cart_v1",browser.Storage.Keys); // Bag still present during POST.
            return new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new GuestReceipt("GQ-11111111111111111111111111111111",12.50m,contact,[new("Cup",12.50m,1)],"Demo — no payment taken","Demo order confirmed"))};
        });
        var shop=new ShopService(http,browser);await shop.Add(1);var result=await shop.GuestCheckout(contact);
        Assert.Equal("GQ-11111111111111111111111111111111",result.Reference);Assert.Equal(0,shop.CartCount);Assert.DoesNotContain("buyer@example.test",string.Join("",browser.Storage.Values));
        var state=new GuestCartStore(browser);await state.Load();Assert.Empty(state.State.Lines);Assert.NotNull(shop.GuestConfirmation);
    }
    [Fact]public async Task FailedCheckoutPreservesBagAndRetryKey()
    {
        var browser=new BrowserMemory();var shop=new ShopService(Http(_=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable){Content=JsonContent.Create(new{message="Try later"})})),browser);
        await shop.Add(1);var before=browser.Storage.Values.Single();await Assert.ThrowsAsync<InvalidOperationException>(()=>shop.GuestCheckout(new GuestContact()));Assert.Equal(before,browser.Storage.Values.Single());Assert.Null(shop.GuestConfirmation);Assert.Equal(1,shop.CartCount);
    }
    [Fact]public void RetryKeyChangesOnlyWithCartMutations()
    {
        var state=new GuestCartState();state.Add(1);var key=state.CheckoutKey;Assert.Equal(key,state.CheckoutKey);state.Set(1,3);Assert.NotEqual(key,state.CheckoutKey);state.Remove(1);Assert.Empty(state.Lines);
    }
    [Fact]public async Task LoginMergeFailurePreservesGuestCartAndSuccessfulRetryClearsIt()
    {
        var browser=new BrowserMemory();var fail=true;
        var http=Http(request=>Task.FromResult(request.RequestUri!.AbsolutePath switch {
            "/api/auth/login"=>new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new{Token="test-session-only",Email="demo@example.test"})},
            "/api/cart/merge"=>new HttpResponseMessage(fail?HttpStatusCode.ServiceUnavailable:HttpStatusCode.NoContent),
            _=>new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(Array.Empty<CartItem>())}
        }));
        var shop=new ShopService(http,browser);await shop.Add(1);Assert.Null(await shop.Login("demo@example.test","not-stored"));Assert.True(shop.HasPendingGuestCart);
        var saved=browser.Storage["localStorage:shopqevora_guest_cart_v1"];Assert.Contains("ProductID",saved);fail=false;await shop.MergeGuestCart();Assert.False(shop.HasPendingGuestCart);Assert.DoesNotContain("not-stored",string.Join("",browser.Storage.Values));
    }

    [Fact]public async Task IncompleteSuccessfulResponseDoesNotClearGuestBag()
    {
        var browser=new BrowserMemory();var shop=new ShopService(Http(_=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new{})})),browser);await shop.Add(1);var before=browser.Storage.Values.Single();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>shop.GuestCheckout(new GuestContact()));Assert.Equal(before,browser.Storage.Values.Single());Assert.Null(shop.GuestConfirmation);
    }

    [Fact]public async Task LogoutCannotReactivateAStalePreRebrandSession()
    {
        var browser=new BrowserMemory();browser.Storage["sessionStorage:shopqevora_token"]="current-test-session";browser.Storage["sessionStorage:shopease_token"]="legacy-test-session";
        var http=Http(_=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(Array.Empty<CartItem>())}));
        var shop=new ShopService(http,browser);await shop.Initialize();Assert.DoesNotContain("sessionStorage:shopease_token",browser.Storage.Keys);await shop.Logout();var refreshed=new ShopService(http,browser);await refreshed.Initialize();Assert.False(refreshed.LoggedIn);
    }

}
