namespace ShopQevora.Client;

public static class ProductPresentation
{
    public sealed record Visual(string File, string Alt);
    public static IReadOnlyDictionary<string, Visual> CatalogVisuals { get; } = new Dictionary<string, Visual>(StringComparer.OrdinalIgnoreCase)
    {
        ["Studio Headphones"] = new("headphones", "Forest green over-ear headphones with ivory ear cushions"),
        ["Minimal Desk Lamp"] = new("lamp", "Minimal gray metal task lamp with an angled shade"),
        ["Everyday Backpack"] = new("backpack", "Olive canvas backpack with a front zip pocket"),
        ["Smart Watch"] = new("watch", "Forest green smart watch with a circular dark face and fabric strap"),
        ["Ceramic Coffee Cup"] = new("cup", "Speckled ivory ceramic mug with a curved handle"),
        ["Canvas Sneakers"] = new("shoes", "Off-white canvas sneakers with cream rubber soles"),
        ["Pocket Sound Speaker"] = new("speaker", "Charcoal woven portable speaker without branding"),
        ["Sculpture Ceramic Vase"] = new("vase", "Sculptural ivory ceramic vase with a curved silhouette"),
        ["Evening Ritual Candle"] = new("candle", "Lit candle in an unbranded frosted ivory glass vessel"),
        ["Daily Hydration Bottle"] = new("bottle", "Forest green reusable bottle with matching cap"),
        ["Weekender Travel Organizer"] = new("travel", "Open sand travel pouch with organized compartments"),
        ["Market Canvas Tote"] = new("tote", "Natural canvas tote with forest green handles"),
        ["Weekend Sneakers"] = new("shoes", "Minimal off-white everyday canvas sneakers"),
        ["Everyday Cotton Cap"] = new("cap", "Sand cotton cap with a curved brim"),
        ["Sunday Sunglasses"] = new("sunglasses", "Tortoiseshell sunglasses with green lenses and rounded frames"),
        ["Slow Morning Hoodie"] = new("hoodie", "Oatmeal cotton hoodie laid flat against an ivory surface"),
        ["Studio Desk Organizer"] = new("organizer", "Walnut desktop organizer with pencils and divided trays"),
        ["Daily Notes Journal"] = new("notebook", "Closed forest green linen notebook with a ribbon bookmark"),
        ["Quiet Wireless Keyboard"] = new("keyboard", "Ivory wireless keyboard in a sage body with simple keycaps"),
        ["Desktop Charging Station"] = new("charging", "Forest green desktop charging dock with phone and watch stands"),
        ["Arc Over-Ear Stand"] = new("headphone-stand", "A curved walnut stand for keeping headphones within reach."),
        ["Linen Cushion Cover"] = new("cushion", "A textured oatmeal cushion cover for relaxed spaces."),
        ["Pebble Catchall Tray"] = new("tray", "A shallow ivory ceramic tray for everyday small essentials."),
        ["Pour Over Coffee Set"] = new("coffee-set", "An ivory ceramic dripper and matching glass server for slow mornings."),
        ["Ribbed Glass Carafe"] = new("carafe", "A clear ribbed glass carafe with a simple cork stopper."),
        ["Foldaway Travel Duffel"] = new("duffel", "A sand canvas overnight bag with forest green handles."),
        ["Passport Folio"] = new("passport", "A compact olive folio for documents and travel essentials."),
        ["Cloud Travel Pillow"] = new("pillow", "A soft sage neck pillow for taking a comfortable break."),
        ["Everyday Crossbody"] = new("crossbody", "A small forest green crossbody bag with an adjustable strap."),
        ["Braided Cotton Belt"] = new("belt", "A natural woven cotton belt with a brushed rectangular buckle."),
        ["Ribbed Knit Beanie"] = new("beanie", "An oatmeal knit beanie with a simple folded cuff."),
        ["Essential Cotton Shirt"] = new("shirt", "An ivory long-sleeve cotton shirt with a relaxed silhouette."),
        ["Cork Yoga Block"] = new("yoga-block", "A natural cork block for supporting comfortable movement."),
        ["Woven Stretch Strap"] = new("stretch-strap", "A sand woven exercise strap with a simple metal loop."),
        ["Slim Laptop Sleeve"] = new("laptop-sleeve", "A sage felt sleeve for organizing a compact laptop."),
        ["Pocket Audio Earbuds"] = new("earbuds", "Forest green wireless earbuds in a matching compact case."),

    };
    // Only images in the audited local manifest are exposed in the public grid.
    // Unknown catalog entries remain in MySQL and are never deleted.
    public static bool HasCatalogImage(Product product) => CatalogVisuals.ContainsKey(product.Name) || KnownPath(product.ImageUrl);
    private static bool KnownPath(string path) => CatalogVisuals.Values.Any(v => path == $"/images/products/{v.File}.webp");
    public static string Image(Product product) => CatalogVisuals.TryGetValue(product.Name, out var visual)
        ? $"/images/products/{visual.File}.webp"
        : KnownPath(product.ImageUrl) ? product.ImageUrl : "/images/product-unavailable.svg";
    public static string? SourceSet(Product product)
    {
        var image = Image(product);
        return image.EndsWith(".webp", StringComparison.Ordinal) ? $"{image[..^5]}-400.webp 400w, {image} 800w" : null;
    }
    public static string Alt(Product product) => CatalogVisuals.TryGetValue(product.Name, out var visual) ? visual.Alt : $"{product.Name} — product image";
    public static string CategoryLabel(string category) => category == "Home" ? "Home & Living" : category;
    public static IEnumerable<Product> Filter(IEnumerable<Product> products, string search, string category, string sort, decimal? minPrice=null, decimal? maxPrice=null)
    {
        var query = products.Where(p => (!minPrice.HasValue || p.Price>=minPrice) && (!maxPrice.HasValue || p.Price<=maxPrice) && (string.IsNullOrWhiteSpace(search) || (p.Name + " " + p.Description).Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(category) || p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)));
        return sort switch { "low" => query.OrderBy(p => p.Price), "high" => query.OrderByDescending(p => p.Price), "name" => query.OrderBy(p => p.Name), "new" => query.OrderByDescending(p => p.ProductID), "old" => query.OrderBy(p=>p.ProductID), "name-desc" => query.OrderByDescending(p=>p.Name), _ => query.OrderBy(p => p.ProductID) };
    }
}
