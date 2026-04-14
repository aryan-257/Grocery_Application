using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

/// <summary>
/// Seeds the OrderService database with a default set of discount coupons on startup.
/// Only runs if the Coupons table is empty, making it safe to call on every startup.
/// Provides a variety of coupon types (percentage and fixed) with different minimum order amounts.
/// </summary>
public static class OrderSeeder
{
    /// <summary>
    /// Inserts the default coupon set if no coupons exist in the database.
    /// Creates 5 coupons covering different discount strategies and expiry windows.
    /// </summary>
    /// <param name="db">The OrderDbContext instance to seed.</param>
    public static async Task SeedAsync(OrderDbContext db)
    {
        if (await db.Coupons.AnyAsync()) return;

        db.Coupons.AddRange(
            new Coupon { Code = "WELCOME10", DiscountType = "Percentage", DiscountValue = 10m, MinOrderAmount = 200m, ExpiresAt = DateTime.UtcNow.AddMonths(6),  IsActive = true, UsageLimit = 100 },
            new Coupon { Code = "SAVE50",    DiscountType = "Fixed",      DiscountValue = 50m, MinOrderAmount = 300m, ExpiresAt = DateTime.UtcNow.AddMonths(3),  IsActive = true, UsageLimit = 50  },
            new Coupon { Code = "FRESH20",   DiscountType = "Percentage", DiscountValue = 20m, MinOrderAmount = 500m, ExpiresAt = DateTime.UtcNow.AddMonths(2),  IsActive = true, UsageLimit = 30  },
            new Coupon { Code = "FLAT100",   DiscountType = "Fixed",      DiscountValue = 100m, MinOrderAmount = 800m, ExpiresAt = DateTime.UtcNow.AddMonths(1), IsActive = true, UsageLimit = 20  },
            new Coupon { Code = "NEWUSER15", DiscountType = "Percentage", DiscountValue = 15m, MinOrderAmount = 100m, ExpiresAt = DateTime.UtcNow.AddMonths(12), IsActive = true, UsageLimit = 200 }
        );
        await db.SaveChangesAsync();
    }
}
