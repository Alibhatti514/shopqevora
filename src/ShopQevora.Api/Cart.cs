using Microsoft.EntityFrameworkCore;
namespace ShopQevora.Api;
// Database-backed cart; the database is named Shop and catalog is stored in Products.
public class Cart(ShopDb db,string userId){
 public async Task AddProduct(Product product){var existing=await db.CartItems.SingleOrDefaultAsync(x=>x.UserId==userId&&x.ProductID==product.ProductID);if(existing==null)db.CartItems.Add(new CartItem{UserId=userId,ProductID=product.ProductID,Quantity=1});else if(existing.Quantity<99)existing.Quantity++;await db.SaveChangesAsync();}
 public async Task RemoveProduct(int productId){var item=await db.CartItems.SingleOrDefaultAsync(x=>x.UserId==userId&&x.ProductID==productId);if(item!=null){db.CartItems.Remove(item);await db.SaveChangesAsync();}}
 public async Task<List<CartItem>> DisplayCartItems()=>await db.CartItems.AsNoTracking().Include(x=>x.Product).Where(x=>x.UserId==userId).ToListAsync();
 public async Task<decimal> CalculateTotal()=> (await DisplayCartItems()).Sum(x=>x.Product.Price*x.Quantity);
}
