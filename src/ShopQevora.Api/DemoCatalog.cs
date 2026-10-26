using Microsoft.EntityFrameworkCore;
namespace ShopQevora.Api;
public static class DemoCatalog
{
 public static IReadOnlyList<Product> Products => new Product[] {
new Product { Name="Studio Headphones", Price=129m, Category="Electronics", Description="Wireless sound with everyday comfort", ImageUrl="/images/products/headphones.webp" },
new Product { Name="Minimal Desk Lamp", Price=79m, Category="Home", Description="Warm light for focused work", ImageUrl="/images/products/lamp.webp" },
new Product { Name="Everyday Backpack", Price=94m, Category="Lifestyle", Description="A durable companion for daily essentials", ImageUrl="/images/products/backpack.webp" },
new Product { Name="Smart Watch", Price=199m, Category="Electronics", Description="Stay connected on the move", ImageUrl="/images/products/watch.webp" },
new Product { Name="Ceramic Coffee Cup", Price=24m, Category="Home", Description="Simple, timeless drinkware", ImageUrl="/images/products/cup.webp" },
new Product { Name="Canvas Sneakers", Price=89m, Category="Lifestyle", Description="Comfort for every step", ImageUrl="/images/products/shoes.webp" },
new Product { Name="Pocket Sound Speaker", Price=59m, Category="Electronics", Description="Room-filling sound in a portable silhouette", ImageUrl="/images/products/speaker.webp" },
new Product { Name="Sculpture Ceramic Vase", Price=42m, Category="Home", Description="A quiet sculptural accent for your favorite stems", ImageUrl="/images/products/vase.webp" },
new Product { Name="Evening Ritual Candle", Price=28m, Category="Home", Description="Make space for a slower evening", ImageUrl="/images/products/candle.webp" },
new Product { Name="Daily Hydration Bottle", Price=32m, Category="Lifestyle", Description="Refill, refresh, repeat. A reusable daily essential", ImageUrl="/images/products/bottle.webp" },
new Product { Name="Weekender Travel Organizer", Price=38m, Category="Accessories", Description="Keep the little things together on every journey", ImageUrl="/images/products/travel.webp" },
new Product { Name="Market Canvas Tote", Price=29m, Category="Lifestyle", Description="An effortless carryall for the everyday", ImageUrl="/images/products/tote.webp" },
new Product { Name="Weekend Sneakers", Price=95m, Category="Fashion", Description="A clean silhouette for days on the go", ImageUrl="/images/products/shoes.webp" },
new Product { Name="Everyday Cotton Cap", Price=25m, Category="Fashion", Description="Your finishing touch for sun-filled days", ImageUrl="/images/products/cap.webp" },
new Product { Name="Sunday Sunglasses", Price=49m, Category="Accessories", Description="A classic frame with a contemporary point of view", ImageUrl="/images/products/sunglasses.webp" },
new Product { Name="Slow Morning Hoodie", Price=78m, Category="Fashion", Description="Soft layers for your off-duty uniform", ImageUrl="/images/products/hoodie.webp" },
new Product { Name="Studio Desk Organizer", Price=35m, Category="Workspace", Description="A considered home for your desk essentials", ImageUrl="/images/products/organizer.webp" },
new Product { Name="Daily Notes Journal", Price=18m, Category="Workspace", Description="A fresh page for your next good idea", ImageUrl="/images/products/notebook.webp" },
new Product { Name="Quiet Wireless Keyboard", Price=89m, Category="Workspace", Description="A more comfortable rhythm for your working day", ImageUrl="/images/products/keyboard.webp" },
new Product { Name="Desktop Charging Station", Price=55m, Category="Electronics", Description="A dedicated place to recharge your everyday devices", ImageUrl="/images/products/charging.webp" },
new Product { Name="Arc Over-Ear Stand", Price=34m, Category="Workspace", Description="A curved walnut stand for keeping headphones within reach.", ImageUrl="/images/products/headphone-stand.webp" },
new Product { Name="Linen Cushion Cover", Price=26m, Category="Home", Description="A textured oatmeal cushion cover for relaxed spaces.", ImageUrl="/images/products/cushion.webp" },
new Product { Name="Pebble Catchall Tray", Price=22m, Category="Home", Description="A shallow ivory ceramic tray for everyday small essentials.", ImageUrl="/images/products/tray.webp" },
new Product { Name="Pour Over Coffee Set", Price=46m, Category="Home", Description="An ivory ceramic dripper and matching glass server for slow mornings.", ImageUrl="/images/products/coffee-set.webp" },
new Product { Name="Ribbed Glass Carafe", Price=38m, Category="Home", Description="A clear ribbed glass carafe with a simple cork stopper.", ImageUrl="/images/products/carafe.webp" },
new Product { Name="Foldaway Travel Duffel", Price=68m, Category="Travel", Description="A sand canvas overnight bag with forest green handles.", ImageUrl="/images/products/duffel.webp" },
new Product { Name="Passport Folio", Price=32m, Category="Travel", Description="A compact olive folio for documents and travel essentials.", ImageUrl="/images/products/passport.webp" },
new Product { Name="Cloud Travel Pillow", Price=29m, Category="Travel", Description="A soft sage neck pillow for taking a comfortable break.", ImageUrl="/images/products/pillow.webp" },
new Product { Name="Everyday Crossbody", Price=62m, Category="Accessories", Description="A small forest green crossbody bag with an adjustable strap.", ImageUrl="/images/products/crossbody.webp" },
new Product { Name="Braided Cotton Belt", Price=24m, Category="Accessories", Description="A natural woven cotton belt with a brushed rectangular buckle.", ImageUrl="/images/products/belt.webp" },
new Product { Name="Ribbed Knit Beanie", Price=23m, Category="Fashion", Description="An oatmeal knit beanie with a simple folded cuff.", ImageUrl="/images/products/beanie.webp" },
new Product { Name="Essential Cotton Shirt", Price=58m, Category="Fashion", Description="An ivory long-sleeve cotton shirt with a relaxed silhouette.", ImageUrl="/images/products/shirt.webp" },
new Product { Name="Cork Yoga Block", Price=19m, Category="Wellness", Description="A natural cork block for supporting comfortable movement.", ImageUrl="/images/products/yoga-block.webp" },
new Product { Name="Woven Stretch Strap", Price=16m, Category="Wellness", Description="A sand woven exercise strap with a simple metal loop.", ImageUrl="/images/products/stretch-strap.webp" },
new Product { Name="Slim Laptop Sleeve", Price=39m, Category="Workspace", Description="A sage felt sleeve for organizing a compact laptop.", ImageUrl="/images/products/laptop-sleeve.webp" },
new Product { Name="Pocket Audio Earbuds", Price=64m, Category="Electronics", Description="Forest green wireless earbuds in a matching compact case.", ImageUrl="/images/products/earbuds.webp" }
 };
 // Add missing demo names; repair only recognized starter image URLs.
 // All IDs, names, prices, descriptions, user images, carts and orders remain intact.
 private static readonly Dictionary<string,string[]> StarterImages = new(StringComparer.OrdinalIgnoreCase) {
  ["Studio Headphones"] = new[] { "/images/products/headphones.jpg", "/images/headphones.svg" },
  ["Minimal Desk Lamp"] = new[] { "/images/products/lamp.jpg", "/images/lamp.svg" },
  ["Everyday Backpack"] = new[] { "/images/products/backpack.jpg", "/images/backpack.svg" },
  ["Smart Watch"] = new[] { "/images/products/watch.jpg", "/images/watch.svg" },
  ["Ceramic Coffee Cup"] = new[] { "/images/products/cup.jpg", "/images/cup.svg" },
  ["Canvas Sneakers"] = new[] { "/images/products/shoes.jpg", "/images/shoes.svg" },
  ["Pocket Sound Speaker"] = new[] { "/images/products/speaker.jpg" },
  ["Sculpture Ceramic Vase"] = new[] { "/images/products/vase.jpg" },
  ["Evening Ritual Candle"] = new[] { "/images/products/candle.jpg" },
  ["Daily Hydration Bottle"] = new[] { "/images/products/bottle.jpg" },
  ["Weekender Travel Organizer"] = new[] { "/images/products/backpack.jpg" },
  ["Market Canvas Tote"] = new[] { "/images/products/tote.jpg" },
  ["Weekend Sneakers"] = new[] { "/images/products/shoes.jpg" },
  ["Everyday Cotton Cap"] = new[] { "/images/products/cap.jpg" },
  ["Sunday Sunglasses"] = new[] { "/images/products/sunglasses.jpg" },
  ["Slow Morning Hoodie"] = new[] { "/images/products/hoodie.jpg" },
  ["Studio Desk Organizer"] = new[] { "/images/products/notebook.jpg" },
  ["Daily Notes Journal"] = new[] { "/images/products/notebook.jpg" },
  ["Quiet Wireless Keyboard"] = new[] { "/images/products/keyboard.jpg" },
  ["Desktop Charging Station"] = new[] { "/images/products/watch.jpg" },
 };
 public static async Task SeedAsync(ShopDb db) {
  var existing = await db.Products.ToListAsync();
  var names = existing.Select(p=>p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
  foreach(var product in Products) {
   if(names.Add(product.Name)) db.Products.Add(product);
   else foreach(var row in existing.Where(p=>p.Name.Equals(product.Name,StringComparison.OrdinalIgnoreCase)))
    if(StarterImages.TryGetValue(product.Name,out var recognized) && recognized.Contains(row.ImageUrl,StringComparer.Ordinal)) row.ImageUrl=product.ImageUrl;
  }
  await db.SaveChangesAsync();
 }
}
