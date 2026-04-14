using OrderService.Data;
using OrderService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Controllers;

/// <summary>
/// Provides coupon discovery and validation for the checkout flow.
/// The listing endpoint is public so the frontend can display available promotions.
/// Validation requires authentication to prevent anonymous abuse.
/// </summary>
[ApiController]
[Route("api/v1/coupons")]
public class CouponsController(OrderDbContext db) : ControllerBase
{
    /// <summary>
    /// Returns all currently active, non-expired coupons.
    /// Used by the frontend to display available promotions on the checkout and offers pages.
    /// Accessible by: all users (anonymous).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var coupons = await db.Coupons
            .Where(c => c.IsActive && (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow))
            .Select(c => new CouponDto(c.Code, c.DiscountType, c.DiscountValue, c.MinOrderAmount, c.ExpiresAt))
            .ToListAsync();

        return Ok(coupons);
    }

    /// <summary>
    /// Validates a coupon code against a given order amount and returns the calculated discount.
    /// Checks that the coupon is active, not expired, within its usage limit, and meets the minimum order amount.
    /// Returns a success response with the discount amount, or a failure response with a descriptive message.
    /// Does NOT apply the coupon — application happens during order creation.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpPost("validate")]
    [Authorize]
    public async Task<IActionResult> Validate(CouponValidateRequest req)
    {
        var coupon = await db.Coupons.FirstOrDefaultAsync(c =>
            c.Code == req.Code.ToUpper() && c.IsActive &&
            (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow) &&
            c.UsedCount < c.UsageLimit);

        if (coupon == null)
            return Ok(new CouponValidateResponse(false, "Invalid or expired coupon code", null, 0, 0));

        if (req.OrderAmount < coupon.MinOrderAmount)
            return Ok(new CouponValidateResponse(false, $"Minimum order amount is Rs.{coupon.MinOrderAmount:F0}", null, 0, 0));

        var discountAmount = coupon.DiscountType == "Percentage"
            ? Math.Round(req.OrderAmount * coupon.DiscountValue / 100, 2)
            : Math.Min(coupon.DiscountValue, req.OrderAmount);

        return Ok(new CouponValidateResponse(true, $"Coupon applied! You save Rs.{discountAmount:F2}",
            coupon.DiscountType, coupon.DiscountValue, discountAmount));
    }
}
