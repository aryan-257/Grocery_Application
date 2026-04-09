using NUnit.Framework;

namespace FreshMart.Tests.ProductService;

// Product business logic
public static class ProductHelper
{
    public static decimal GetDiscountedPrice(decimal price, decimal discountPercent) =>
        discountPercent > 0 ? Math.Round(price * (1 - discountPercent / 100m), 2) : price;

    public static bool IsInStock(int stockQuantity) => stockQuantity > 0;

    public static bool IsLowStock(int stockQuantity, int threshold = 10) =>
        stockQuantity > 0 && stockQuantity <= threshold;

    public static bool IsValidSku(string sku) =>
        !string.IsNullOrWhiteSpace(sku) && sku.Length >= 3 && sku.Length <= 20;

    public static bool IsValidPrice(decimal price) => price > 0;

    public static bool IsValidDiscount(decimal discountPercent) =>
        discountPercent >= 0 && discountPercent < 100;

    public static string GetStockStatus(int qty) => qty switch
    {
        0 => "Out of Stock",
        <= 10 => "Low Stock",
        _ => "In Stock"
    };
}

[TestFixture]
public class ProductValidationTests
{
    [Test]
    public void GetDiscountedPrice_NoDiscount_ReturnsOriginal()
    {
        Assert.That(ProductHelper.GetDiscountedPrice(100m, 0), Is.EqualTo(100m));
    }

    [Test]
    public void GetDiscountedPrice_10Percent_IsCorrect()
    {
        Assert.That(ProductHelper.GetDiscountedPrice(100m, 10), Is.EqualTo(90m));
    }

    [Test]
    public void GetDiscountedPrice_20Percent_IsCorrect()
    {
        Assert.That(ProductHelper.GetDiscountedPrice(130m, 20), Is.EqualTo(104m));
    }

    [Test]
    public void GetDiscountedPrice_RoundsToTwoDecimals()
    {
        Assert.That(ProductHelper.GetDiscountedPrice(99m, 15), Is.EqualTo(84.15m));
    }

    [Test]
    public void IsInStock_PositiveQty_ReturnsTrue()
    {
        Assert.That(ProductHelper.IsInStock(5), Is.True);
    }

    [Test]
    public void IsInStock_ZeroQty_ReturnsFalse()
    {
        Assert.That(ProductHelper.IsInStock(0), Is.False);
    }

    [Test]
    public void IsLowStock_BelowThreshold_ReturnsTrue()
    {
        Assert.That(ProductHelper.IsLowStock(5), Is.True);
    }

    [Test]
    public void IsLowStock_AboveThreshold_ReturnsFalse()
    {
        Assert.That(ProductHelper.IsLowStock(50), Is.False);
    }

    [Test]
    public void IsLowStock_ZeroQty_ReturnsFalse()
    {
        Assert.That(ProductHelper.IsLowStock(0), Is.False);
    }

    [Test]
    public void IsValidSku_ValidSku_ReturnsTrue()
    {
        Assert.That(ProductHelper.IsValidSku("PA001"), Is.True);
    }

    [Test]
    public void IsValidSku_TooShort_ReturnsFalse()
    {
        Assert.That(ProductHelper.IsValidSku("AB"), Is.False);
    }

    [Test]
    public void IsValidSku_Empty_ReturnsFalse()
    {
        Assert.That(ProductHelper.IsValidSku(""), Is.False);
    }

    [Test]
    public void IsValidPrice_Positive_ReturnsTrue()
    {
        Assert.That(ProductHelper.IsValidPrice(50m), Is.True);
    }

    [Test]
    public void IsValidPrice_Zero_ReturnsFalse()
    {
        Assert.That(ProductHelper.IsValidPrice(0m), Is.False);
    }

    [Test]
    public void IsValidDiscount_Zero_ReturnsTrue()
    {
        Assert.That(ProductHelper.IsValidDiscount(0), Is.True);
    }

    [Test]
    public void IsValidDiscount_100Percent_ReturnsFalse()
    {
        Assert.That(ProductHelper.IsValidDiscount(100), Is.False);
    }

    [Test]
    public void GetStockStatus_Zero_IsOutOfStock()
    {
        Assert.That(ProductHelper.GetStockStatus(0), Is.EqualTo("Out of Stock"));
    }

    [Test]
    public void GetStockStatus_LowQty_IsLowStock()
    {
        Assert.That(ProductHelper.GetStockStatus(5), Is.EqualTo("Low Stock"));
    }

    [Test]
    public void GetStockStatus_HighQty_IsInStock()
    {
        Assert.That(ProductHelper.GetStockStatus(100), Is.EqualTo("In Stock"));
    }
}
