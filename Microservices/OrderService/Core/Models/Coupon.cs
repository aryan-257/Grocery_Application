namespace OrderService.Core.Models;

/// <summary>
/// Discount coupon that can be applied at checkout.
/// Supports two types: Percentage (e.g. 10% off) and Fixed (e.g. ₹50 off).
/// </summary>
public class Coupon
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The code customer types at checkout, e.g. WELCOME10. Stored in uppercase.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Either "Percentage" or "Fixed".</summary>
    public string DiscountType { get; set; } = "Percentage";

    public decimal DiscountValue { get; set; }

    public decimal MinOrderAmount { get; set; }

    /// <summary>Null means no expiry.</summary>
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public int UsageLimit { get; set; } = 100;

    public int UsedCount { get; set; }
}
