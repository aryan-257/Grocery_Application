using Microsoft.EntityFrameworkCore;
using ProductService.Core.Models;

namespace ProductService.Infrastructure.Data;

/// <summary>
/// DB context for ProductService.
/// </summary>
public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Review> Reviews => Set<Review>();

    // local copy of user info — saved when a review is submitted so we dont
    // have to call AuthService just to show the reviewer's name
    public DbSet<AppUser> Users => Set<AppUser>();

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
    }
}
