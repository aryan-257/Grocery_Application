using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Data;

/// <summary>
/// Entity Framework Core database context for the PaymentService.
/// Manages <see cref="Payment"/> records and configures the enum-to-integer
/// conversion for <see cref="PaymentStatus"/> storage.
/// </summary>
public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    /// <summary>
    /// All payment transactions in the system.
    /// Each record corresponds to a Razorpay order created for a FreshMart order.
    /// </summary>
    public DbSet<Payment> Payments => Set<Payment>();

    /// <summary>
    /// Configures the <see cref="PaymentStatus"/> enum to be stored as an integer
    /// in the database for efficient querying and storage.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>()
            .Property(p => p.Status)
            .HasConversion<int>();
    }
}
