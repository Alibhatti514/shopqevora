using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopQevora.Api;
using ShopQevora.Contracts;
using Xunit;
namespace ShopQevora.Tests;

public class ContactTests
{
    static ContactRequest Valid(ContactChallenge challenge)=>new(){FullName="Fictional Customer",Email="contact@example.test",Phone="+92 300 1234567",Category="order",OrderReference="GQ-"+new string('A',32),Message="A fictional question about the demo store.",SubmissionKey=challenge.SubmissionKey,FormToken=challenge.FormToken};
    static async Task<ContactChallenge> Challenge(HttpClient client)=>(await client.GetFromJsonAsync<ContactChallenge>("/api/contact/challenge"))!;
    [Theory][InlineData("name")][InlineData("email")][InlineData("phone")][InlineData("category")][InlineData("message")][InlineData("order")][InlineData("html")]
    public void SharedValidationRejectsMissingMalformedOrOverlongFields(string field)
    {
        var input=Valid(new(new string('a',32),"token"));
        switch(field){case "name":input.FullName=" ";break;case "email":input.Email="invalid";break;case "phone":input.Phone="03001234567";break;case "category":input.Category="unsupported";break;case "message":input.Message=new string('x',4001);break;case "order":input.OrderReference=new string('x',65);break;case "html":input.Message="<script>unsafe</script>";break;}
        Assert.False(Validator.TryValidateObject(input,new ValidationContext(input),new List<ValidationResult>(),true));
    }
    [Fact]public async Task GuestSubmissionPersistsNormalizedPhoneAndReturnsOnlyPrivateSafeReceipt()
    {
        await using var factory=new GuestApiFactory();using var client=factory.CreateClient();var input=Valid(await Challenge(client));
        var response=await client.PostAsJsonAsync("/api/contact/inquiries",input);Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        var text=await response.Content.ReadAsStringAsync();Assert.DoesNotContain(input.Email,text);Assert.DoesNotContain(input.Message,text);var receipt=(await response.Content.ReadFromJsonAsync<ContactReceipt>())!;Assert.Matches("^CQ-[A-F0-9]{32}$",receipt.Reference);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ShopDb>();var saved=await db.ContactInquiries.SingleAsync();Assert.Equal("+923001234567",saved.Phone);Assert.Null(saved.UserId);Assert.Equal(input.Message,saved.Message);Assert.Equal(input.OrderReference,saved.OrderReference);
        foreach(var path in new[]{"/api/contact/inquiries","/api/contact/inquiries/1","/api/contact/inquiries/"+receipt.Reference}){var lookup=await client.GetAsync(path);Assert.False(lookup.IsSuccessStatusCode);Assert.DoesNotContain(input.Email,await lookup.Content.ReadAsStringAsync());}
    }
    [Fact]public async Task SignedCustomerAttributionComesOnlyFromClaimsAndOthersCannotReadInquiry()
    {
        await using var factory=new GuestApiFactory();using var client=factory.CreateClient();var input=Valid(await Challenge(client));client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",factory.Token("alice"));
        Assert.Equal(HttpStatusCode.Created,(await client.PostAsJsonAsync("/api/contact/inquiries",new{input.FullName,input.Email,input.Phone,input.Category,input.OrderReference,input.Message,input.SubmissionKey,input.FormToken,UserId="bob"})).StatusCode);
        using var scope=factory.Services.CreateScope();Assert.Equal("alice",(await scope.ServiceProvider.GetRequiredService<ShopDb>().ContactInquiries.SingleAsync()).UserId);
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",factory.Token("bob"));Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/api/contact/inquiries/1")).StatusCode);
    }
    [Fact]public async Task SpamTrapTamperedTokenAndInvalidPhoneNeverCreateRows()
    {
        await using var factory=new GuestApiFactory();using var client=factory.CreateClient();var input=Valid(await Challenge(client));input.Website="https://spam.example";Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/contact/inquiries",input)).StatusCode);
        input.Website="";input.FormToken="tampered";Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/contact/inquiries",input)).StatusCode);
        input=Valid(await Challenge(client));input.Phone="+999 123456789";Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/contact/inquiries",input)).StatusCode);
        using var scope=factory.Services.CreateScope();Assert.Empty(await scope.ServiceProvider.GetRequiredService<ShopDb>().ContactInquiries.ToListAsync());
    }
    [Fact]public async Task DuplicateRetriesAndRefreshedSessionsAreIdempotentButChangedPayloadConflicts()
    {
        await using var factory=new GuestApiFactory();using var client=factory.CreateClient();var input=Valid(await Challenge(client));var first=await client.PostAsJsonAsync("/api/contact/inquiries",input);Assert.Equal(HttpStatusCode.Created,first.StatusCode);var receipt=await first.Content.ReadFromJsonAsync<ContactReceipt>();
        var refresh=await client.GetFromJsonAsync<ContactChallenge>("/api/contact/challenge?submissionKey="+input.SubmissionKey);Assert.Equal(input.SubmissionKey,refresh!.SubmissionKey);input.FormToken=refresh.FormToken;
        var second=await client.PostAsJsonAsync("/api/contact/inquiries",input);Assert.Equal(HttpStatusCode.OK,second.StatusCode);Assert.Equal(receipt,await second.Content.ReadFromJsonAsync<ContactReceipt>());
        input.Message="A different fictional message to prevent duplicate key reuse.";Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/contact/inquiries",input)).StatusCode);
        using var scope=factory.Services.CreateScope();Assert.Single(await scope.ServiceProvider.GetRequiredService<ShopDb>().ContactInquiries.ToListAsync());
    }
    [Fact]public async Task ReasonableSubmissionRateLimitRejectsSixthAttempt()
    {
        await using var factory=new GuestApiFactory();using var client=factory.CreateClient();for(var i=0;i<5;i++)Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/contact/inquiries",new ContactRequest())).StatusCode);
        var blocked=await client.PostAsJsonAsync("/api/contact/inquiries",new ContactRequest());Assert.Equal(HttpStatusCode.TooManyRequests,blocked.StatusCode);Assert.True(blocked.Headers.RetryAfter?.Delta>TimeSpan.Zero);
    }
    [Fact]public void TokensExpireAndCannotMoveBetweenSubmissionKeys()
    {
        var clock=new TestClock();var tokens=new ContactFormTokens(new EphemeralDataProtectionProvider(),clock);var issued=tokens.Issue();Assert.True(tokens.Valid(issued.SubmissionKey,issued.FormToken));Assert.False(tokens.Valid(Guid.NewGuid().ToString("N"),issued.FormToken));clock.Now=clock.Now.AddMinutes(21);Assert.False(tokens.Valid(issued.SubmissionKey,issued.FormToken));var refresh=tokens.Issue(issued.SubmissionKey);Assert.True(tokens.Valid(refresh.SubmissionKey,refresh.FormToken));
    }
    sealed class TestClock:TimeProvider{public DateTimeOffset Now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Now;}
}
