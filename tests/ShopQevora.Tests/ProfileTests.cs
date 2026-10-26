using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ShopQevora.Api;
using Xunit;
namespace ShopQevora.Tests;
public class ProfileTests {
 [Theory][InlineData("03001234567","PK","+923001234567")][InlineData("2025550123","US","+12025550123")][InlineData("bad","PK",null)][InlineData("","PK","")]
 public void PhonesNormalizeWithoutClaimingVerification(string number,string region,string? expected)=>Assert.Equal(expected,ProfileEndpoints.NormalizePhone(number,region));
 static ProfileInput Profile()=>new("Test Customer","Reader","03001234567","PK","Demo Street","Karachi","Sindh","74000","PK");
 [Fact]public async Task ProfilesRequireAuthenticationAndNeverAcceptAnOwnerId(){
  await using var factory=new GuestApiFactory();using var client=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/account/profile")).StatusCode);
  var email="profile@example.test";var password="Secret123!";await client.PostAsJsonAsync("/api/auth/register",new{email,password});var login=await client.PostAsJsonAsync("/api/auth/login",new{email,password});using var json=System.Text.Json.JsonDocument.Parse(await login.Content.ReadAsStringAsync());client.DefaultRequestHeaders.Authorization=new("Bearer",json.RootElement.GetProperty("token").GetString());
  Assert.Equal(HttpStatusCode.NoContent,(await client.PutAsJsonAsync("/api/account/profile",Profile())).StatusCode);
  Assert.Contains("+923001234567",await client.GetStringAsync("/api/account/profile"));
  Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync("/api/account/profile",Profile() with {FullName="<script>unsafe</script>"})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync("/api/account/profile",Profile() with {Phone="invalid"})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/account/avatar",new{ContentType="image/png",Data=Convert.ToBase64String("<script>bad</script>"u8.ToArray())})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/account/password",new{CurrentPassword="wrong",NewPassword="Other123!"})).StatusCode);
  Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/account/email",new{Email="pending@example.test",CurrentPassword=password})).StatusCode);
  var body=await client.GetStringAsync("/api/account/profile");Assert.Contains("pending@example.test",body);Assert.Contains(email,body);Assert.Contains("\"emailVerified\":false",body);
  client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",factory.Token("nonexistent"));Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/account/profile")).StatusCode);
 }
 [Fact]public async Task AvatarIsDecodedReencodedAndRemovableAndPasswordRevokesNewSession(){
  await using var factory=new GuestApiFactory();using var client=factory.CreateClient();var email="avatar@example.test";var password="Password123!";await client.PostAsJsonAsync("/api/auth/register",new{email,password});var login=await client.PostAsJsonAsync("/api/auth/login",new{email,password});using var json=System.Text.Json.JsonDocument.Parse(await login.Content.ReadAsStringAsync());client.DefaultRequestHeaders.Authorization=new("Bearer",json.RootElement.GetProperty("token").GetString());
  using var bitmap=new SkiaSharp.SKBitmap(10,10);using var image=SkiaSharp.SKImage.FromBitmap(bitmap);using var png=image.Encode(SkiaSharp.SKEncodedImageFormat.Png,90);
  Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsJsonAsync("/api/account/avatar",new{ContentType="image/png",Data=Convert.ToBase64String(png.ToArray())})).StatusCode);Assert.Contains("data:image/png;base64,",await client.GetStringAsync("/api/account/profile"));
  Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/account/avatar",new{ContentType="image/jpeg",Data=Convert.ToBase64String(png.ToArray())})).StatusCode);
  Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync("/api/account/avatar")).StatusCode);Assert.DoesNotContain("data:image/png;base64,",await client.GetStringAsync("/api/account/profile"));
  Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/account/password",new{CurrentPassword=password,NewPassword="Different123!"})).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/account/profile")).StatusCode);
 }
 [Fact]public async Task MalformedGuestJsonReturnsClientErrorRatherThanServerFailure(){await using var app=new GuestApiFactory();using var client=app.CreateClient();var response=await client.PostAsync("/api/guest-orders",new StringContent("{invalid",System.Text.Encoding.UTF8,"application/json"));Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);}
 [Fact]public async Task ProfileUpdatesExcludeLargeReadOnlyAvatarAndIdentityFields(){var handler=new CaptureProfileHandler();using var http=new HttpClient(handler){BaseAddress=new Uri("https://unit.test/")};var service=new ShopQevora.Client.ShopService(http,null!);Assert.Null(await service.SaveProfile(new ShopQevora.Client.CustomerProfile{FullName="Owner",Avatar=new string('x',200000),Email="private@example.test",PendingEmail="pending@example.test"}));Assert.True(handler.Body.Length<2048);using var body=System.Text.Json.JsonDocument.Parse(handler.Body);Assert.False(body.RootElement.TryGetProperty("avatar",out _));Assert.False(body.RootElement.TryGetProperty("email",out _));Assert.Equal("Owner",body.RootElement.GetProperty("fullName").GetString());}
 sealed class CaptureProfileHandler:HttpMessageHandler{public string Body="";protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){Body=await request.Content!.ReadAsStringAsync(token);return new HttpResponseMessage(HttpStatusCode.NoContent);}}
 [Fact]public void PriceRangeAndDescendingAlphabeticalCombine(){var products=new[]{new ShopQevora.Client.Product{Name="A",Category="Home",Price=10},new ShopQevora.Client.Product{Name="B",Category="Home",Price=25},new ShopQevora.Client.Product{Name="C",Category="Home",Price=30},new ShopQevora.Client.Product{Name="D",Category="Other",Price=30}};Assert.Equal(new[]{"C","B"},ShopQevora.Client.ProductPresentation.Filter(products,"","Home","name-desc",20,30).Select(p=>p.Name));Assert.Empty(ShopQevora.Client.ProductPresentation.Filter(products,"","","low",40,10));}
}
