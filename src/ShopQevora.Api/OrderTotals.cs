namespace ShopQevora.Api;
public static class OrderTotals {
 public static decimal Calculate(IEnumerable<CartItem> items) => items.Sum(x=>x.Product.Price*x.Quantity);
}
