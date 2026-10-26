using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace ShopQevora.Api;
public class ShopDb(DbContextOptions<ShopDb> options):IdentityDbContext<AppUser>(options){
 public DbSet<ContactInquiry> ContactInquiries=>Set<ContactInquiry>();
 public DbSet<CustomerProfile> CustomerProfiles=>Set<CustomerProfile>();
 public DbSet<GuestCheckout> GuestCheckouts=>Set<GuestCheckout>();
 public DbSet<Product> Products=>Set<Product>(); public DbSet<CartItem> CartItems=>Set<CartItem>(); public DbSet<Order> Orders=>Set<Order>(); public DbSet<OrderLine> OrderLines=>Set<OrderLine>();
 protected override void OnModelCreating(ModelBuilder b){base.OnModelCreating(b);b.Entity<ContactInquiry>().HasIndex(x=>x.Reference).IsUnique();b.Entity<ContactInquiry>().HasIndex(x=>x.SubmissionKeyHash).IsUnique();b.Entity<CustomerProfile>().HasOne<AppUser>().WithOne().HasForeignKey<CustomerProfile>(x=>x.UserId).OnDelete(DeleteBehavior.Cascade);b.Entity<GuestCheckout>().HasKey(x=>x.OrderId);b.Entity<GuestCheckout>().HasOne(x=>x.Order).WithOne().HasForeignKey<GuestCheckout>(x=>x.OrderId).OnDelete(DeleteBehavior.Restrict);b.Entity<GuestCheckout>().HasIndex(x=>x.Reference).IsUnique();b.Entity<GuestCheckout>().HasIndex(x=>x.CheckoutKeyHash).IsUnique();b.Entity<Product>().HasKey(x=>x.ProductID);b.Entity<Product>().Property(x=>x.Price).HasPrecision(12,2);b.Entity<CartItem>().HasIndex(x=>new{x.UserId,x.ProductID}).IsUnique();b.Entity<Order>().Property(x=>x.Total).HasPrecision(12,2);b.Entity<OrderLine>().Property(x=>x.UnitPrice).HasPrecision(12,2);}
}
