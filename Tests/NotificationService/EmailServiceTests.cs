using NUnit.Framework;

namespace FreshMart.Tests.NotificationService;

// Inline order info models
public record OrderItemInfo(string ProductName, int Quantity, decimal UnitPrice);
public record OrderInfo(Guid Id, decimal TotalAmount, IEnumerable<OrderItemInfo> Items, DateTime? EstimatedDelivery = null);

// Extracted pure logic from EmailService for testing
public static class EmailTemplateHelper
{
    public static string ShortId(Guid id) => id.ToString()[..8].ToUpper();

    public static string FormatTotal(decimal amount) => $"₹{amount:F2}";

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try { var addr = new System.Net.Mail.MailAddress(email); return addr.Address == email; }
        catch { return false; }
    }

    public static decimal CalcItemTotal(OrderItemInfo item) => item.Quantity * item.UnitPrice;

    public static decimal CalcOrderItemsTotal(IEnumerable<OrderItemInfo> items) =>
        items.Sum(i => CalcItemTotal(i));
}

[TestFixture]
public class EmailServiceTests
{
    [Test]
    public void ShortId_Returns8CharUppercase()
    {
        var id = Guid.NewGuid();
        var shortId = EmailTemplateHelper.ShortId(id);
        Assert.That(shortId.Length, Is.EqualTo(8));
        Assert.That(shortId, Is.EqualTo(shortId.ToUpper()));
    }

    [Test]
    public void ShortId_IsDeterministic()
    {
        var id = Guid.Parse("12345678-0000-0000-0000-000000000000");
        Assert.That(EmailTemplateHelper.ShortId(id), Is.EqualTo("12345678"));
    }

    [Test]
    public void FormatTotal_FormatsCorrectly()
    {
        Assert.That(EmailTemplateHelper.FormatTotal(299.50m), Is.EqualTo("₹299.50"));
    }

    [Test]
    public void FormatTotal_ZeroAmount()
    {
        Assert.That(EmailTemplateHelper.FormatTotal(0m), Is.EqualTo("₹0.00"));
    }

    [Test]
    public void IsValidEmail_ValidEmail_ReturnsTrue()
    {
        Assert.That(EmailTemplateHelper.IsValidEmail("kajaldalal081@gmail.com"), Is.True);
    }

    [Test]
    public void IsValidEmail_EmptyString_ReturnsFalse()
    {
        Assert.That(EmailTemplateHelper.IsValidEmail(""), Is.False);
    }

    [Test]
    public void IsValidEmail_NoAtSign_ReturnsFalse()
    {
        Assert.That(EmailTemplateHelper.IsValidEmail("notanemail"), Is.False);
    }

    [Test]
    public void IsValidEmail_NullString_ReturnsFalse()
    {
        Assert.That(EmailTemplateHelper.IsValidEmail(null!), Is.False);
    }

    [Test]
    public void CalcItemTotal_QuantityTimesPrice()
    {
        var item = new OrderItemInfo("Apple", 3, 50m);
        Assert.That(EmailTemplateHelper.CalcItemTotal(item), Is.EqualTo(150m));
    }

    [Test]
    public void CalcOrderItemsTotal_SumsAllItems()
    {
        var items = new[]
        {
            new OrderItemInfo("Apple", 2, 50m),   // 100
            new OrderItemInfo("Milk", 1, 65m),    // 65
            new OrderItemInfo("Bread", 3, 35m),   // 105
        };
        Assert.That(EmailTemplateHelper.CalcOrderItemsTotal(items), Is.EqualTo(270m));
    }

    [Test]
    public void CalcOrderItemsTotal_EmptyList_ReturnsZero()
    {
        Assert.That(EmailTemplateHelper.CalcOrderItemsTotal([]), Is.EqualTo(0m));
    }

    [Test]
    public void OrderInfo_CanBeCreated()
    {
        var id = Guid.NewGuid();
        var items = new[] { new OrderItemInfo("Mango", 1, 250m) };
        var order = new OrderInfo(id, 250m, items);

        Assert.That(order.Id, Is.EqualTo(id));
        Assert.That(order.TotalAmount, Is.EqualTo(250m));
        Assert.That(order.Items.Count(), Is.EqualTo(1));
    }
}
