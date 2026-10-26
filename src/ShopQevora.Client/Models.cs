namespace ShopQevora.Client;
public class Product {public int ProductID{get;set;} public string Name{get;set;}="";public decimal Price{get;set;} public string Category{get;set;}="";public string Description{get;set;}="";public string ImageUrl{get;set;}="";}
public class CartItem {public int Id{get;set;} public int ProductID{get;set;} public Product Product{get;set;}=new();public int Quantity{get;set;}}
public class Order {public int Id{get;set;} public DateTime CreatedUtc{get;set;} public decimal Total{get;set;} public List<OrderLine> Lines{get;set;}=new();}
public class OrderLine{public string ProductName{get;set;}="";public int Quantity{get;set;}public decimal UnitPrice{get;set;}}

public sealed class GuestContact
{
 [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(120,MinimumLength=2)] public string FullName {get;set;}="";
 [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress, System.ComponentModel.DataAnnotations.StringLength(254)] public string Email {get;set;}="";
 [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.RegularExpression(@"^\+?[0-9][0-9 ()-]{6,28}[0-9]$")] public string Phone {get;set;}="";
 [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(500,MinimumLength=5)] public string Address {get;set;}="";
 [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(80)] public string City {get;set;}="";
 [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(80)] public string Region {get;set;}="";
 [System.ComponentModel.DataAnnotations.StringLength(20)] public string PostalCode {get;set;}="";
 public string Country {get;set;}="PK";
}
public sealed record GuestReceiptLine(string ProductName,decimal UnitPrice,int Quantity);
public sealed record GuestReceipt(string Reference,decimal Total,GuestContact Contact,List<GuestReceiptLine> Lines,string PaymentStatus,string Status);
