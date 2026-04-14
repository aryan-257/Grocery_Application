namespace SupportService.Models;

/// <summary>
/// Represents a customer support ticket in the FreshMart platform.
/// Tickets are created by customers to report issues and are managed by Admin and StoreManager staff.
/// Each ticket has a category, priority, and lifecycle status, and contains a thread of messages.
/// </summary>
public class SupportTicket
{
    /// <summary>Unique identifier for the support ticket (primary key).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ID of the customer who opened the ticket.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Full name of the customer, snapshotted at ticket creation time.
    /// Stored directly to avoid a cross-service lookup when displaying the ticket to staff.
    /// </summary>
    public string CustomerName { get; set; } = "";

    /// <summary>
    /// Email of the customer, snapshotted at ticket creation time.
    /// Used for potential email notifications about ticket updates.
    /// </summary>
    public string CustomerEmail { get; set; } = "";

    /// <summary>Brief description of the issue, used as the ticket title in listings.</summary>
    public string Subject { get; set; } = "";

    /// <summary>
    /// Category of the issue to help staff route and prioritise tickets.
    /// Valid values: <c>Order</c>, <c>Payment</c>, <c>Delivery</c>, <c>Product</c>, <c>Other</c>.
    /// </summary>
    public string Category { get; set; } = "Other"; // Order, Payment, Delivery, Product, Other

    /// <summary>
    /// Current lifecycle status of the ticket.
    /// Valid values: <c>Open</c>, <c>InProgress</c>, <c>Resolved</c>, <c>Closed</c>.
    /// Automatically transitions to <c>InProgress</c> when staff first replies.
    /// </summary>
    public string Status { get; set; } = "Open";    // Open, InProgress, Resolved, Closed

    /// <summary>
    /// Urgency level of the ticket, used to sort and filter the staff queue.
    /// Valid values: <c>Low</c>, <c>Medium</c>, <c>High</c>.
    /// </summary>
    public string Priority { get; set; } = "Medium"; // Low, Medium, High

    /// <summary>UTC timestamp of when the ticket was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last update to the ticket (status change or new message).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of when the ticket was resolved or closed. Null while still open.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>The thread of messages exchanged between the customer and support staff.</summary>
    public ICollection<SupportMessage> Messages { get; set; } = [];
}
