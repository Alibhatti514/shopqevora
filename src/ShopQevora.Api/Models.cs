using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Identity;
namespace ShopQevora.Api;
public class Product {
 public int ProductID {get;set;}
 [Required, StringLength(120)] public string Name {get;set;}="";
 [Range(0.01,1000000)] public decimal Price {get;set;}
 [Required, StringLength(60)] public string Category {get;set;}="";
 public string Description {get;set;}="";
 public string ImageUrl {get;set;}="";
 public string DisplayDetails()=> $"Product: {Name} | Price: {Price.ToString("C",CultureInfo.GetCultureInfo("en-US"))} | Category: {Category}";
}
public class CartItem { public int Id {get;set;} public string UserId {get;set;}=""; public int ProductID {get;set;} public Product Product {get;set;}=null!; public int Quantity {get;set;}=1; }
public class Order { public int Id {get;set;} public string UserId {get;set;}=""; public DateTime CreatedUtc {get;set;}=DateTime.UtcNow; public decimal Total {get;set;} public List<OrderLine> Lines {get;set;}=new(); }
public class OrderLine {public int Id {get;set;} public int OrderId {get;set;} public string ProductName {get;set;}=""; public decimal UnitPrice {get;set;} public int Quantity {get;set;} }
public class AppUser:IdentityUser {}
public record AuthRequest([property: Required,EmailAddress]string Email,[property: Required]string Password);
public record RegisterRequest([property: Required,EmailAddress]string Email,[property: Required,MinLength(8)]string Password);
public record QuantityRequest([property: Range(1,99)]int Quantity);
