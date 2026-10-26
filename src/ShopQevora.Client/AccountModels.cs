namespace ShopQevora.Client;
public class CustomerProfile {
 public string FullName{get;set;}="";public string DisplayName{get;set;}="";public string Email{get;set;}="";public bool EmailVerified{get;set;}
 public string Phone{get;set;}="";public string PhoneRegion{get;set;}="PK";public string Address{get;set;}="";public string City{get;set;}="";public string Region{get;set;}="";public string PostalCode{get;set;}="";public string Country{get;set;}="PK";public string? Avatar{get;set;}public string? PendingEmail{get;set;}
}
public record DialingCode(string Region,int Code){public string CountryName {get{try{return new System.Globalization.RegionInfo(Region).EnglishName;}catch{return Region;}}}}
