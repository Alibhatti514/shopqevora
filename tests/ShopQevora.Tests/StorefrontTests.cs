using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ShopQevora.Api;
using Xunit;
namespace ShopQevora.Tests;
public class StorefrontTests {
 private static ShopDb Database()=>new(new DbContextOptionsBuilder<ShopDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
 [Theory][InlineData(0,false)][InlineData(-1,false)][InlineData(100,false)][InlineData(1,true)][InlineData(99,true)]
 public void QuantityRange(int quantity,bool valid)=>Assert.Equal(valid,RequestValidation.Valid(new QuantityRequest(quantity)));
 [Theory][InlineData("bad", "Password1!",false)][InlineData("a@example.com","short",false)][InlineData("a@example.com","Password1!",true)]
 public void RegistrationValidation(string email,string password,bool valid)=>Assert.Equal(valid,RequestValidation.Valid(new RegisterRequest(email,password)));
 [Fact]public void ProductRejectsInvalidPrice(){var product=new Product{Name="Cup",Category="Home",Price=-1};Assert.False(RequestValidation.Valid(product));}
 [Fact]public async Task SeedIsIdempotentAndPreservesExistingRows(){await using var db=Database();db.Products.Add(new Product{ProductID=50,Name="Studio Headphones",Price=777,Category="Custom",Description="Preserve me"});await db.SaveChangesAsync();await DemoCatalog.SeedAsync(db);await DemoCatalog.SeedAsync(db);Assert.Equal(36,await db.Products.CountAsync());var original=await db.Products.FindAsync(50);Assert.Equal(777,original!.Price);Assert.Equal("Preserve me",original.Description);}
 [Fact]public async Task CartCapsAt99AndCalculatesDecimalTotals(){await using var db=Database();var p=new Product{Name="Cup",Price=19.99m,Category="Home"};db.Products.Add(p);await db.SaveChangesAsync();var cart=new Cart(db,"a");for(var i=0;i<101;i++)await cart.AddProduct(p);Assert.Equal(99,(await cart.DisplayCartItems()).Single().Quantity);Assert.Equal(1979.01m,await cart.CalculateTotal());}
 [Fact]public void SearchCategoryAndPriceSortWorkTogether(){var items=new[]{new ShopQevora.Client.Product{Name="Warm lamp",Description="Studio",Category="Home",Price=40},new ShopQevora.Client.Product{Name="Studio cup",Category="Home",Price=20},new ShopQevora.Client.Product{Name="Studio headset",Category="Electronics",Price=10}};var filtered=ShopQevora.Client.ProductPresentation.Filter(items,"studio","Home","low").ToArray();Assert.Equal(2,filtered.Length);Assert.Equal("Studio cup",filtered[0].Name);Assert.Empty(ShopQevora.Client.ProductPresentation.Filter(items,"missing","","low"));}
 [Fact]public void OrderTotalUsesServerProductPrices(){var items=new[]{new CartItem{Product=new Product{Price=19.99m},Quantity=3},new CartItem{Product=new Product{Price=2.50m},Quantity=2}};Assert.Equal(64.97m,OrderTotals.Calculate(items));}
}
