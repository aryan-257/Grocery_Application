using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

/// <summary>
/// Entity Framework Core database context for the OrderService.
/// Manages orders, cart state, coupons, and local read-model projections
/// for users and products (to avoid synchronous cross-service calls at checkout).
/// </summary>
public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    /// <summary>All customer orders placed on the platform.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Individual line items belonging to orders.</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <summary>Active customer shopping carts (one per customer).</summary>
    public DbSet<Cart> Carts => Set<Cart>();

    /// <summary>Individual items within customer carts.</summary>
    public DbSet<CartItem> CartItems => Set<CartItem>();

    /// <summary>Discount coupons available for use at checkout.</summary>
    public DbSet<Coupon> Coupons => Set<Coupon>();

    /// <summary>
    /// Local projection of AuthService users.
    /// Populated on order creation to enable email lookups for status notifications
    /// without calling AuthService at notification time.
    /// </summary>
    public DbSet<AppUser> Users => Set<AppUser>();

    /// <summary>
    /// Local cache of ProductService products.
    /// Populated when items are added to the cart so prices and images
    /// are available without a live call to ProductService.
    /// </summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>
    /// Configures entity relationships and cascade delete behaviors.
    /// Ensures cart items are cascade-deleted with their cart,
    /// and prevents accidental cascade deletes on product references.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cart>()
            .HasMany(c => c.Items)
            .WithOne(i => i.Cart)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
