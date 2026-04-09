using NUnit.Framework;

namespace FreshMart.Tests.SupportService;

// Inline models
public class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Category { get; set; } = "Other";
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Medium";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public List<SupportMessage> Messages { get; set; } = [];
}

public class SupportMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = "";
    public string SenderRole { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsStaff { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Business logic helper
public static class SupportHelper
{
    public static readonly string[] ValidStatuses = ["Open", "InProgress", "Resolved", "Closed"];
    public static readonly string[] ValidPriorities = ["Low", "Medium", "High"];
    public static readonly string[] ValidCategories = ["Order", "Payment", "Delivery", "Product", "Other"];

    public static bool IsValidStatus(string status) => ValidStatuses.Contains(status);
    public static bool IsValidPriority(string priority) => ValidPriorities.Contains(priority);
    public static bool IsValidCategory(string category) => ValidCategories.Contains(category);

    public static bool CanTransition(string from, string to) => (from, to) switch
    {
        ("Open", "InProgress") => true,
        ("Open", "Closed") => true,
        ("InProgress", "Resolved") => true,
        ("InProgress", "Closed") => true,
        ("Resolved", "Closed") => true,
        _ => false
    };

    public static SupportTicket Resolve(SupportTicket ticket)
    {
        ticket.Status = "Resolved";
        ticket.ResolvedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;
        return ticket;
    }
}

[TestFixture]
public class SupportTicketTests
{
    [Test]
    public void SupportTicket_DefaultStatus_IsOpen()
    {
        var ticket = new SupportTicket();
        Assert.That(ticket.Status, Is.EqualTo("Open"));
    }

    [Test]
    public void SupportTicket_DefaultPriority_IsMedium()
    {
        var ticket = new SupportTicket();
        Assert.That(ticket.Priority, Is.EqualTo("Medium"));
    }

    [Test]
    public void SupportTicket_DefaultCategory_IsOther()
    {
        var ticket = new SupportTicket();
        Assert.That(ticket.Category, Is.EqualTo("Other"));
    }

    [Test]
    public void SupportTicket_NewTicket_HasNoMessages()
    {
        var ticket = new SupportTicket();
        Assert.That(ticket.Messages, Is.Empty);
    }

    [Test]
    public void SupportTicket_NewTicket_ResolvedAtIsNull()
    {
        var ticket = new SupportTicket();
        Assert.That(ticket.ResolvedAt, Is.Null);
    }

    [Test]
    public void IsValidStatus_ValidValues_ReturnsTrue()
    {
        Assert.That(SupportHelper.IsValidStatus("Open"), Is.True);
        Assert.That(SupportHelper.IsValidStatus("InProgress"), Is.True);
        Assert.That(SupportHelper.IsValidStatus("Resolved"), Is.True);
        Assert.That(SupportHelper.IsValidStatus("Closed"), Is.True);
    }

    [Test]
    public void IsValidStatus_InvalidValue_ReturnsFalse()
    {
        Assert.That(SupportHelper.IsValidStatus("Pending"), Is.False);
        Assert.That(SupportHelper.IsValidStatus(""), Is.False);
    }

    [Test]
    public void IsValidPriority_ValidValues_ReturnsTrue()
    {
        Assert.That(SupportHelper.IsValidPriority("Low"), Is.True);
        Assert.That(SupportHelper.IsValidPriority("Medium"), Is.True);
        Assert.That(SupportHelper.IsValidPriority("High"), Is.True);
    }

    [Test]
    public void IsValidCategory_ValidValues_ReturnsTrue()
    {
        foreach (var cat in new[] { "Order", "Payment", "Delivery", "Product", "Other" })
            Assert.That(SupportHelper.IsValidCategory(cat), Is.True);
    }

    [Test]
    public void CanTransition_OpenToInProgress_IsAllowed()
    {
        Assert.That(SupportHelper.CanTransition("Open", "InProgress"), Is.True);
    }

    [Test]
    public void CanTransition_InProgressToResolved_IsAllowed()
    {
        Assert.That(SupportHelper.CanTransition("InProgress", "Resolved"), Is.True);
    }

    [Test]
    public void CanTransition_ResolvedToOpen_IsNotAllowed()
    {
        Assert.That(SupportHelper.CanTransition("Resolved", "Open"), Is.False);
    }

    [Test]
    public void CanTransition_ClosedToAny_IsNotAllowed()
    {
        Assert.That(SupportHelper.CanTransition("Closed", "Open"), Is.False);
        Assert.That(SupportHelper.CanTransition("Closed", "InProgress"), Is.False);
    }

    [Test]
    public void Resolve_SetsStatusAndResolvedAt()
    {
        var ticket = new SupportTicket { Status = "InProgress" };
        var before = DateTime.UtcNow;
        SupportHelper.Resolve(ticket);
        Assert.That(ticket.Status, Is.EqualTo("Resolved"));
        Assert.That(ticket.ResolvedAt, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void SupportMessage_IsStaff_DefaultFalse()
    {
        var msg = new SupportMessage();
        Assert.That(msg.IsStaff, Is.False);
    }

    [Test]
    public void SupportMessage_CanAddToTicket()
    {
        var ticket = new SupportTicket();
        ticket.Messages.Add(new SupportMessage { Message = "Hello", SenderName = "Kajal" });
        Assert.That(ticket.Messages.Count, Is.EqualTo(1));
        Assert.That(ticket.Messages[0].Message, Is.EqualTo("Hello"));
    }
}
