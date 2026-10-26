using Microsoft.EntityFrameworkCore;
using ShopQevora.Api;
using Xunit;
namespace ShopQevora.Tests;
public class GuestOrderTests
{
    public static GuestOrderRequest Valid()=>new(){CheckoutKey=Guid.NewGuid().ToString("N"),Contact=new(){FullName="Demo Buyer",Email="buyer@example.test",Phone="+92 300 1234567",Address="House 12, Demo Street",City="Karachi",Region="Sindh",Country="PK"},Lines=[new(1,2)]};
    static ShopDb Database()=>new(new DbContextOptionsBuilder<ShopDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    [Fact]public async Task CreatesGuestOrderWithAuthoritativePricesAndPreservesExistingOrders()
    {
        await using var db=Database();db.Products.Add(new Product{ProductID=1,Name="Cup",Price=19.99m,Category="Home"});db.Orders.Add(new Order{UserId="alice",Total=5,Lines=[new(){ProductName="Existing",Quantity=1,UnitPrice=5}]});await db.SaveChangesAsync();
        var receipt=await new GuestOrderService(db).Place(Valid());Assert.Equal(39.98m,receipt.Total);Assert.StartsWith("GQ-",receipt.Reference);Assert.Equal(35,receipt.Reference.Length);Assert.Equal(2,await db.Orders.CountAsync());Assert.Equal(5,(await db.Orders.SingleAsync(x=>x.UserId=="alice")).Total);Assert.Single(await db.GuestCheckouts.ToListAsync());Assert.DoesNotContain("paid",receipt.PaymentStatus.ToLowerInvariant());
    }
    [Fact]public async Task RetryReturnsSameOrderAndChangedRequestIsRejected()
    {
        await using var db=Database();db.Products.Add(new Product{ProductID=1,Name="Cup",Price=10,Category="Home"});await db.SaveChangesAsync();var service=new GuestOrderService(db);var request=Valid();var first=await service.Place(request);var again=await service.Place(request);Assert.Equal(first.Reference,again.Reference);Assert.Single(await db.Orders.ToListAsync());request.Lines=[new(1,3)];await Assert.ThrowsAsync<CheckoutConflictException>(()=>service.Place(request));
    }
    [Fact]public async Task RejectsUnknownProductsWithoutWritingAnyOrder()
    {
        await using var db=Database();await Assert.ThrowsAsync<CheckoutValidationException>(()=>new GuestOrderService(db).Place(Valid()));Assert.Empty(await db.Orders.ToListAsync());
    }
    [Theory][InlineData(0)][InlineData(100)][InlineData(-5)]public async Task RejectsInvalidQuantities(int quantity)
    {
        await using var db=Database();var request=Valid();request.Lines=[new(1,quantity)];await Assert.ThrowsAsync<CheckoutValidationException>(()=>new GuestOrderService(db).Place(request));Assert.Empty(await db.Orders.ToListAsync());
    }
    [Fact]public void MissingDetailsEmailAndUnsupportedCountryAreRejected()
    {
        var request=Valid();request.Contact=null;Assert.NotEmpty(request.Validate());request=Valid();request.Contact!.Email="invalid";Assert.NotEmpty(request.Validate());request=Valid();request.Contact!.Country="US";Assert.NotEmpty(request.Validate());request=Valid();request.Lines=[new(1,1),new(1,2)];Assert.NotEmpty(request.Validate());
    }
    [Theory][InlineData("FullName")][InlineData("Email")][InlineData("Phone")][InlineData("Address")][InlineData("City")][InlineData("Region")]
    public void EveryRequiredContactFieldIsValidated(string field)
    {
        var request=Valid();typeof(GuestContact).GetProperty(field)!.SetValue(request.Contact,"");Assert.NotEmpty(request.Validate());
    }

}
