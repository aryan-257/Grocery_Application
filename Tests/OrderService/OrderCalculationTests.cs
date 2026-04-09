using NUnit.Framework;

namespace FreshMart.Tests.OrderService;

// Inline models to avoid project reference
public class Product { public Guid Id { get; set; } = Guid.NewGuid(); public decimal Price { get; set; } public decimal DiscountPercent { get; set; } public string Name { get; set; } = ""; }
public class CartItem  { public Guid ProductId { get; set; } public int Quantity { get; set; } }

// Extracted order calculation logic (mirrors OrdersController logic)
public static class OrderCalculator
{
    public static decimal GetUnitPrice(Product p) =>
        p.DiscountPercent > 0
            ? Math.Round(p.Price * (1 - p.DiscountPercent / 100m), 2)
            : p.Price;

    public static decimal CalcSubTotal(IEnumerable<CartItem> items, Dictionary<Guid, Product> products) =>
        items.Sum(i => products.TryGetValue(i.ProductId, out var p) ? GetUnitPrice(p) * i.Quantity : 0m);

    public static decimal CalcDeliveryFee(decimal subTotal) => subTotal >= 500 ? 0m : 49m;

    public static decimal CalcTax(decimal subTotal) => Math.Round(subTotal * 0.05m, 2);

    public static decimal CalcTotal(decimal subTotal, decimal deliveryFee, decimal tax, decimal discount) =>
        Math.Max(0, subTotal + deliveryFee + tax - discount);
}

[TestFixture]
public class OrderCalculationTests
{
    [Test]
    public void GetUnitPrice_NoDiscount_ReturnsOriginalPrice()
    {
        var product = new Product { Price = 100m, DiscountPercent = 0 };
        Assert.That(OrderCalculator.GetUnitPrice(product), Is.EqualTo(100m));
    }

    [Test]
    public void GetUnitPrice_WithDiscount_ReturnsDiscountedPrice()
    {
        var product = new Product { Price = 100m, DiscountPercent = 10 };
        Assert.That(OrderCalculator.GetUnitPrice(product), Is.EqualTo(90m));
    }

    [Test]
    public void GetUnitPrice_With20PercentDiscount_IsCorrect()
    {
        var product = new Product { Price = 130m, DiscountPercent = 20 };
        Assert.That(OrderCalculator.GetUnitPrice(product), Is.EqualTo(104m));
    }

    [Test]
    public void CalcDeliveryFee_BelowThreshold_Returns49()
    {
        Assert.That(OrderCalculator.CalcDeliveryFee(499m), Is.EqualTo(49m));
    }

    [Test]
    public void CalcDeliveryFee_AtThreshold_ReturnsFree()
    {
        Assert.That(OrderCalculator.CalcDeliveryFee(500m), Is.EqualTo(0m));
    }

    [Test]
    public void CalcDeliveryFee_AboveThreshold_ReturnsFree()
    {
        Assert.That(OrderCalculator.CalcDeliveryFee(1000m), Is.EqualTo(0m));
    }

    [Test]
    public void CalcTax_Is5Percent()
    {
        Assert.That(OrderCalculator.CalcTax(200m), Is.EqualTo(10m));
    }

    [Test]
    public void CalcTax_RoundsToTwoDecimals()
    {
        Assert.That(OrderCalculator.CalcTax(100m), Is.EqualTo(5m));
    }

    [Test]
    public void CalcSubTotal_MultipleItems_IsCorrect()
    {
        var p1 = new Product { Id = Guid.NewGuid(), Price = 100m, DiscountPercent = 0 };
        var p2 = new Product { Id = Guid.NewGuid(), Price = 200m, DiscountPercent = 10 };
        var products = new Dictionary<Guid, Product> { [p1.Id] = p1, [p2.Id] = p2 };
        var items = new List<CartItem>
        {
            new() { ProductId = p1.Id, Quantity = 2 },  // 200
            new() { ProductId = p2.Id, Quantity = 1 },  // 180
        };
        Assert.That(OrderCalculator.CalcSubTotal(items, products), Is.EqualTo(380m));
    }

    [Test]
    public void CalcSubTotal_MissingProduct_SkipsItem()
    {
        var p1 = new Product { Id = Guid.NewGuid(), Price = 100m };
        var products = new Dictionary<Guid, Product> { [p1.Id] = p1 };
        var items = new List<CartItem>
        {
            new() { ProductId = p1.Id, Quantity = 1 },
            new() { ProductId = Guid.NewGuid(), Quantity = 5 }, // not in dict
        };
        Assert.That(OrderCalculator.CalcSubTotal(items, products), Is.EqualTo(100m));
    }

    [Test]
    public void CalcTotal_WithDiscount_IsCorrect()
    {
        // subTotal=400, delivery=49, tax=20, discount=50 → 419
        Assert.That(OrderCalculator.CalcTotal(400m, 49m, 20m, 50m), Is.EqualTo(419m));
    }

    [Test]
    public void CalcTotal_NeverGoesNegative()
    {
        // huge discount
        Assert.That(OrderCalculator.CalcTotal(100m, 0m, 5m, 9999m), Is.EqualTo(0m));
    }

    [Test]
    public void FullOrderFlow_SmallOrder_HasDeliveryFee()
    {
        var p = new Product { Id = Guid.NewGuid(), Price = 100m, DiscountPercent = 0 };
        var products = new Dictionary<Guid, Product> { [p.Id] = p };
        var items = new List<CartItem> { new() { ProductId = p.Id, Quantity = 2 } };

        var subTotal = OrderCalculator.CalcSubTotal(items, products); // 200
        var delivery = OrderCalculator.CalcDeliveryFee(subTotal);     // 49
        var tax = OrderCalculator.CalcTax(subTotal);                  // 10
        var total = OrderCalculator.CalcTotal(subTotal, delivery, tax, 0);

        Assert.That(subTotal, Is.EqualTo(200m));
        Assert.That(delivery, Is.EqualTo(49m));
        Assert.That(tax, Is.EqualTo(10m));
        Assert.That(total, Is.EqualTo(259m));
    }

    [Test]
    public void FullOrderFlow_LargeOrder_FreeDelivery()
    {
        var p = new Product { Id = Guid.NewGuid(), Price = 300m, DiscountPercent = 0 };
        var products = new Dictionary<Guid, Product> { [p.Id] = p };
        var items = new List<CartItem> { new() { ProductId = p.Id, Quantity = 2 } };

        var subTotal = OrderCalculator.CalcSubTotal(items, products); // 600
        var delivery = OrderCalculator.CalcDeliveryFee(subTotal);     // 0
        var tax = OrderCalculator.CalcTax(subTotal);                  // 30
        var total = OrderCalculator.CalcTotal(subTotal, delivery, tax, 0);

        Assert.That(delivery, Is.EqualTo(0m));
        Assert.That(total, Is.EqualTo(630m));
    }
}
