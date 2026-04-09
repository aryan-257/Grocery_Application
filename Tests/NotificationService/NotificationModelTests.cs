using NUnit.Framework;

namespace FreshMart.Tests.NotificationService;

// Inline notification model
public class AppNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "info";
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class NotificationHelper
{
    public static readonly string[] ValidTypes = ["info", "success", "warning", "error", "order"];

    public static bool IsValidType(string type) => ValidTypes.Contains(type);

    public static AppNotification CreateOrderPlaced(Guid userId, string orderId, decimal total) => new()
    {
        UserId = userId,
        Title = "Payment Successful",
        Message = $"Payment for order #{orderId} completed. Total: Rs.{total:F2}",
        Type = "success",
        Link = $"/orders/{orderId}/track"
    };

    public static AppNotification CreateOrderShipped(Guid userId, string orderId) => new()
    {
        UserId = userId,
        Title = "Order Shipped",
        Message = $"Your order #{orderId} has been shipped!",
        Type = "info",
        Link = $"/orders/{orderId}/track"
    };

    public static AppNotification CreateOrderDelivered(Guid userId, string orderId) => new()
    {
        UserId = userId,
        Title = "Order Delivered",
        Message = $"Your order #{orderId} has been delivered. Enjoy!",
        Type = "success",
        Link = $"/orders/{orderId}/track"
    };
}

[TestFixture]
public class NotificationModelTests
{
    [Test]
    public void AppNotification_DefaultIsRead_IsFalse()
    {
        var n = new AppNotification();
        Assert.That(n.IsRead, Is.False);
    }

    [Test]
    public void AppNotification_DefaultType_IsInfo()
    {
        var n = new AppNotification();
        Assert.That(n.Type, Is.EqualTo("info"));
    }

    [Test]
    public void AppNotification_HasUniqueId()
    {
        var n1 = new AppNotification();
        var n2 = new AppNotification();
        Assert.That(n1.Id, Is.Not.EqualTo(n2.Id));
    }

    [Test]
    public void IsValidType_ValidTypes_ReturnTrue()
    {
        foreach (var t in new[] { "info", "success", "warning", "error", "order" })
            Assert.That(NotificationHelper.IsValidType(t), Is.True);
    }

    [Test]
    public void IsValidType_InvalidType_ReturnsFalse()
    {
        Assert.That(NotificationHelper.IsValidType("unknown"), Is.False);
        Assert.That(NotificationHelper.IsValidType(""), Is.False);
    }

    [Test]
    public void CreateOrderPlaced_HasCorrectType()
    {
        var n = NotificationHelper.CreateOrderPlaced(Guid.NewGuid(), "ABC12345", 299.50m);
        Assert.That(n.Type, Is.EqualTo("success"));
    }

    [Test]
    public void CreateOrderPlaced_MessageContainsTotal()
    {
        var n = NotificationHelper.CreateOrderPlaced(Guid.NewGuid(), "ABC12345", 299.50m);
        Assert.That(n.Message, Does.Contain("299.50"));
    }

    [Test]
    public void CreateOrderPlaced_MessageContainsOrderId()
    {
        var n = NotificationHelper.CreateOrderPlaced(Guid.NewGuid(), "ABC12345", 100m);
        Assert.That(n.Message, Does.Contain("ABC12345"));
    }

    [Test]
    public void CreateOrderShipped_HasCorrectTitle()
    {
        var n = NotificationHelper.CreateOrderShipped(Guid.NewGuid(), "XYZ99999");
        Assert.That(n.Title, Is.EqualTo("Order Shipped"));
    }

    [Test]
    public void CreateOrderDelivered_HasCorrectType()
    {
        var n = NotificationHelper.CreateOrderDelivered(Guid.NewGuid(), "XYZ99999");
        Assert.That(n.Type, Is.EqualTo("success"));
    }

    [Test]
    public void CreateOrderPlaced_LinkContainsOrderId()
    {
        var orderId = "ABC12345";
        var n = NotificationHelper.CreateOrderPlaced(Guid.NewGuid(), orderId, 100m);
        Assert.That(n.Link, Does.Contain(orderId));
    }

    [Test]
    public void AppNotification_CanMarkAsRead()
    {
        var n = new AppNotification();
        n.IsRead = true;
        Assert.That(n.IsRead, Is.True);
    }
}
