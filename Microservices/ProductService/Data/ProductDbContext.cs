using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data;

/// <summary>
/// Entity Framework Core database context for the ProductService.
/// Manages products, categories, reviews, and local read-model projections
/// for users and orders (used for review eligibility checks without cross-service calls).
/// </summary>
public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    /// <summary>All grocery products in the FreshMart catalogue.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Product categories used for navigation and filtering.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Customer reviews submitted for products.</summary>
    public DbSet<Review> Reviews => Set<Review>();

    /// <summary>
    /// Local projection of AuthService users.
    /// Populated on demand to resolve reviewer names without calling AuthService.
    /// </summary>
    public DbSet<AppUser> Users => Set<AppUser>();

    /// <summary>
    /// Local projection of orders from OrderService.
    /// Used to verify purchase eligibility before allowing a review submission.
    /// </summary>
    public DbSet<OrderProjection> Orders => Set<OrderProjection>();

    /// <summary>Line items belonging to the local order projections.</summary>
    public DbSet<OrderItemProjection> OrderItems => Set<OrderItemProjection>();

    /// <summary>
    /// Configures entity relationships and cascade delete behaviors.
    /// Prevents accidental cascade deletes on category hierarchy and order projections.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany()
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId);

        modelBuilder.Entity<OrderProjection>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
