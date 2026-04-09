using NUnit.Framework;

namespace FreshMart.Tests.OrderService;

// Inline Coupon model
public class Coupon
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int UsageLimit { get; set; } = 100;
    public int UsedCount { get; set; }
}

// Coupon validation logic (mirrors OrdersController)
public static class CouponHelper
{
    public static bool IsValid(Coupon coupon, decimal subTotal) =>
        coupon.IsActive &&
        (coupon.ExpiresAt == null || coupon.ExpiresAt > DateTime.UtcNow) &&
        coupon.UsedCount < coupon.UsageLimit &&
        subTotal >= coupon.MinOrderAmount;

    public static decimal CalcDiscount(Coupon coupon, decimal subTotal) =>
        coupon.DiscountType == "Percentage"
            ? Math.Round(subTotal * coupon.DiscountValue / 100, 2)
            : Math.Min(coupon.DiscountValue, subTotal);
}

[TestFixture]
public class CouponTests
{
    private Coupon MakeCoupon(string type = "Percentage", decimal value = 10, decimal minOrder = 0) =>
        new() { Code = "TEST10", DiscountType = type, DiscountValue = value, MinOrderAmount = minOrder, IsActive = true, UsageLimit = 100, UsedCount = 0 };

    [Test]
    public void IsValid_ActiveCoupon_ReturnsTrue()
    {
        var coupon = MakeCoupon();
        Assert.That(CouponHelper.IsValid(coupon, 500m), Is.True);
    }

    [Test]
    public void IsValid_InactiveCoupon_ReturnsFalse()
    {
        var coupon = MakeCoupon(); coupon.IsActive = false;
        Assert.That(CouponHelper.IsValid(coupon, 500m), Is.False);
    }

    [Test]
    public void IsValid_ExpiredCoupon_ReturnsFalse()
    {
        var coupon = MakeCoupon(); coupon.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        Assert.That(CouponHelper.IsValid(coupon, 500m), Is.False);
    }

    [Test]
    public void IsValid_FutureCoupon_ReturnsTrue()
    {
        var coupon = MakeCoupon(); coupon.ExpiresAt = DateTime.UtcNow.AddDays(30);
        Assert.That(CouponHelper.IsValid(coupon, 500m), Is.True);
    }

    [Test]
    public void IsValid_UsageLimitReached_ReturnsFalse()
    {
        var coupon = MakeCoupon(); coupon.UsageLimit = 10; coupon.UsedCount = 10;
        Assert.That(CouponHelper.IsValid(coupon, 500m), Is.False);
    }

    [Test]
    public void IsValid_BelowMinOrder_ReturnsFalse()
    {
        var coupon = MakeCoupon(minOrder: 500m);
        Assert.That(CouponHelper.IsValid(coupon, 499m), Is.False);
    }

    [Test]
    public void IsValid_AtMinOrder_ReturnsTrue()
    {
        var coupon = MakeCoupon(minOrder: 500m);
        Assert.That(CouponHelper.IsValid(coupon, 500m), Is.True);
    }

    [Test]
    public void CalcDiscount_Percentage_IsCorrect()
    {
        var coupon = MakeCoupon("Percentage", 10);
        Assert.That(CouponHelper.CalcDiscount(coupon, 500m), Is.EqualTo(50m));
    }

    [Test]
    public void CalcDiscount_Fixed_IsCorrect()
    {
        var coupon = MakeCoupon("Fixed", 100);
        Assert.That(CouponHelper.CalcDiscount(coupon, 500m), Is.EqualTo(100m));
    }

    [Test]
    public void CalcDiscount_Fixed_CannotExceedSubTotal()
    {
        var coupon = MakeCoupon("Fixed", 9999);
        Assert.That(CouponHelper.CalcDiscount(coupon, 200m), Is.EqualTo(200m));
    }

    [Test]
    public void CalcDiscount_Percentage_RoundsToTwoDecimals()
    {
        var coupon = MakeCoupon("Percentage", 15);
        var discount = CouponHelper.CalcDiscount(coupon, 333m); // 49.95
        Assert.That(discount, Is.EqualTo(49.95m));
    }

    [Test]
    public void Coupon_DefaultValues_AreCorrect()
    {
        var coupon = new Coupon();
        Assert.That(coupon.IsActive, Is.True);
        Assert.That(coupon.UsageLimit, Is.EqualTo(100));
        Assert.That(coupon.UsedCount, Is.EqualTo(0));
        Assert.That(coupon.DiscountType, Is.EqualTo("Percentage"));
    }
}
