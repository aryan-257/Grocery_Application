namespace OrderService.Models;

/// <summary>
/// Represents a discount coupon that customers can apply at checkout.
/// Supports both percentage-based and fixed-amount discounts with usage limits and expiry.
/// </summary>
public class Coupon
{
    /// <summary>Unique identifier for the coupon (primary key).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The coupon code entered by the customer at checkout (e.g., <c>WELCOME10</c>).
    /// Stored and matched in uppercase.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Determines how the discount is calculated.
    /// <c>Percentage</c>: deducts a percentage of the subtotal.
    /// <c>Fixed</c>: deducts a fixed INR amount from the subtotal.
    /// </summary>
    public string DiscountType { get; set; } = "Percentage"; // Percentage | Fixed

    /// <summary>
    /// The discount magnitude. Interpreted as a percentage (0–100) for <c>Percentage</c> type,
    /// or as an INR amount for <c>Fixed</c> type.
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Minimum order subtotal (in INR) required for the coupon to be applicable.</summary>
    public decimal MinOrderAmount { get; set; }

    /// <summary>Optional UTC expiry date. Coupons past this date are rejected. Null means no expiry.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Whether the coupon is currently active. Inactive coupons are rejected at validation.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Maximum number of times this coupon can be redeemed across all users.</summary>
    public int UsageLimit { get; set; } = 100;

    /// <summary>Number of times this coupon has been successfully applied. Incremented on each use.</summary>
    public int UsedCount { get; set; }
}
