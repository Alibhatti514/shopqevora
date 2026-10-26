using Microsoft.EntityFrameworkCore;
using ShopQevora.Api;
using ShopQevora.Client;
using Xunit;
using ClientProduct = ShopQevora.Client.Product;
namespace ShopQevora.Tests;

public class VisualContractTests
{
    private static string WebRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while(directory is not null && !File.Exists(Path.Combine(directory.FullName,"ShopQevora.sln"))) directory=directory.Parent;
        return Path.Combine(directory!.FullName,"src","ShopQevora.Client","wwwroot");
    }
    [Fact] public void CarouselLoopsInBothDirections()
    {
        var state=new CarouselState(5);
        state.Previous(); Assert.Equal(4,state.Current);
        state.Next(); Assert.Equal(0,state.Current);
        for(var i=0;i<5;i++)state.Tick(); Assert.Equal(0,state.Current);
        state.GoTo(2); Assert.Equal(2,state.Current);
        Assert.Throws<ArgumentOutOfRangeException>(()=>state.GoTo(5));
    }
    [Fact] public void AutoplayHonorsPauseAndReducedMotion()
    {
        var state=new CarouselState(5){Paused=true};
        state.Tick(); Assert.Equal(0,state.Current);
        state.Paused=false;state.ReducedMotion=true;state.Tick();Assert.Equal(0,state.Current);
        state.ReducedMotion=false;state.Tick();Assert.Equal(1,state.Current);
    }
    [Fact] public void EveryDemoProductHasRelevantLocalResponsiveImages()
    {
        Assert.InRange(DemoCatalog.Products.Count,36,48);
        foreach(var product in DemoCatalog.Products)
        {
            var p=new ClientProduct{Name=product.Name,ImageUrl=product.ImageUrl};
            Assert.True(ProductPresentation.HasCatalogImage(p));
            Assert.Equal(product.ImageUrl,ProductPresentation.Image(p));
            Assert.EndsWith(".webp",ProductPresentation.Image(p));
            Assert.True(File.Exists(WebRoot()+ProductPresentation.Image(p)),$"Missing image for {p.Name}");
            Assert.True(File.Exists(WebRoot()+ProductPresentation.Image(p).Replace(".webp","-400.webp")));
            Assert.Contains("400w",ProductPresentation.SourceSet(p));
            Assert.True(ProductPresentation.Alt(p).Length>20);
        }
    }
    [Fact] public void LegacyIllustrationIsNeverUsedAndUnknownBrokenImagesAreExcluded()
    {
        var legacy=new ClientProduct{Name="Canvas Sneakers",ImageUrl="/images/shoes.svg"};
        Assert.Equal("/images/products/shoes.webp",ProductPresentation.Image(legacy));
        var unknown=new ClientProduct{Name="User item",ImageUrl="https://invalid.example/broken.jpg"};
        Assert.False(ProductPresentation.HasCatalogImage(unknown));
        Assert.Equal("/images/product-unavailable.svg",ProductPresentation.Image(unknown));
    }
    [Fact] public void AllCarouselSlidesHaveBothImageSizesAndValidCategoryTargets()
    {
        Assert.InRange(HeroContent.Slides.Count,4,5);
        Assert.Equal(HeroContent.Slides.Count,HeroContent.Slides.Select(s=>s.Image).Distinct().Count());
        var categories=DemoCatalog.Products.Select(p=>p.Category).Append("").Append("favorites");
        foreach(var slide in HeroContent.Slides)
        {
            Assert.True(File.Exists(Path.Combine(WebRoot(),"images","hero",slide.Image+".webp")));
            Assert.True(File.Exists(Path.Combine(WebRoot(),"images","hero",slide.Image+"-mobile.webp")));
            Assert.Contains(slide.Category,categories);
        }
    }
    [Fact] public async Task ImageRepairUpdatesOnlyRecognizedStarterMetadata()
    {
        await using var db=new ShopDb(new DbContextOptionsBuilder<ShopDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Products.AddRange(new ShopQevora.Api.Product{ProductID=1,Name="Studio Headphones",ImageUrl="/images/headphones.svg",Price=333,Category="Electronics"},new ShopQevora.Api.Product{ProductID=77,Name="Minimal Desk Lamp",ImageUrl="/images/custom-owner-image.jpg",Price=99,Category="Home"});
        await db.SaveChangesAsync();await DemoCatalog.SeedAsync(db);await DemoCatalog.SeedAsync(db);
        Assert.Equal(36,await db.Products.CountAsync());
        Assert.Equal("/images/products/headphones.webp",(await db.Products.FindAsync(1))!.ImageUrl);
        Assert.Equal(333,(await db.Products.FindAsync(1))!.Price);
        Assert.Equal("/images/custom-owner-image.jpg",(await db.Products.FindAsync(77))!.ImageUrl);
    }
}
