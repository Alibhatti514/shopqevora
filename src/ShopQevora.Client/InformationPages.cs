namespace ShopQevora.Client;
public static class InformationPages {
 public record Content(string Title,string Text);
 public static readonly Dictionary<string,Content> Links=new(){
 ["about"]=new("About ShopQevora","ShopQevora is an independent educational e-commerce demonstration built with Blazor, ASP.NET Core and MySQL. The catalog illustrates a considered approach to everyday essentials."),
 ["contact"]=new("Contact Us","The Contact Us demonstration form stores inquiries privately in MySQL. It is not actively monitored, sends no emails and offers no response-time promise."),
 ["shipping"]=new("Shipping Information","Demo information: no products are shipped. Addresses entered at checkout are stored to demonstrate order processing only. No delivery times, coverage or shipping guarantees are offered."),
 ["returns"]=new("Returns and Refunds","Demo information: no real payments are collected and no goods are fulfilled, so this project does not process commercial returns or refunds."),
 ["privacy"]=new("Privacy Policy","Demo notice, not a commercial legal policy: account details, profile information, contact inquiries and demo orders are stored in the project MySQL database. Guest carts are stored in your browser; sign-in tokens are kept in session storage. Avoid entering sensitive real personal information. No advertising or social login integration is configured. Contact the project operator through your project channel for data requests."),
 ["terms"]=new("Terms and Conditions","Demo notice, not commercial legal advice: this storefront is provided for learning and portfolio evaluation. Catalog images are illustrative. Orders are simulations, not purchases, and do not create payment or fulfillment commitments.")};
}
