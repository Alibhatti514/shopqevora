using Microsoft.EntityFrameworkCore;
using ShopQevora.Api;
using Xunit;
namespace ShopQevora.Tests;
public class CartTests{
 private static ShopDb Database(){var options=new DbContextOptionsBuilder<ShopDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;return new ShopDb(options);}
 [Fact]public void ProductDetails_AreFormatted(){var p=new Product{ProductID=1,Name="Laptop",Price=999.99m,Category="Electronics"};Assert.Equal("Product: Laptop | Price: $999.99 | Category: Electronics",p.DisplayDetails());}
 [Fact]public async Task AddRemoveAndTotal(){await using var db=Database();var a=new Product{ProductID=1,Name="Laptop",Price=999.99m,Category="Electronics"};var b=new Product{ProductID=2,Name="Mouse",Price=29.50m,Category="Electronics"};db.Products.AddRange(a,b);await db.SaveChangesAsync();var cart=new Cart(db,"test-user");await cart.AddProduct(a);await cart.AddProduct(b);Assert.Equal(1029.49m,await cart.CalculateTotal());await cart.RemoveProduct(1);Assert.Single(await cart.DisplayCartItems());Assert.Equal(29.50m,await cart.CalculateTotal());}
 [Fact]public async Task SameProduct_IncrementsQuantity(){await using var db=Database();var p=new Product{ProductID=1,Name="Laptop",Price=100m,Category="Electronics"};db.Products.Add(p);await db.SaveChangesAsync();var cart=new Cart(db,"test-user");await cart.AddProduct(p);await cart.AddProduct(p);Assert.Equal(2,(await cart.DisplayCartItems()).Single().Quantity);Assert.Equal(200m,await cart.CalculateTotal());}
 [Fact]public async Task CartsAreSeparatedByUser(){await using var db=Database();var p=new Product{ProductID=1,Name="Laptop",Price=100m,Category="Electronics"};db.Products.Add(p);await db.SaveChangesAsync();await new Cart(db,"alice").AddProduct(p);Assert.Empty(await new Cart(db,"bob").DisplayCartItems());}
}
