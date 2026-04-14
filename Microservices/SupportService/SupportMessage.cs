namespace SupportService.Models;

/// <summary>
/// Represents a single message in a support ticket conversation thread.
/// Messages can be sent by the customer or by support staff (Admin/StoreManager).
/// The <see cref="IsStaff"/> flag is used to visually differentiate staff replies in the UI.
/// </summary>
public class SupportMessage
{
    /// <summary>Unique identifier for the message (primary key).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Foreign key to the parent <see cref="SupportTicket"/>.</summary>
    public Guid TicketId { get; set; }

    /// <summary>ID of the user who sent this message.</summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// Display name of the sender, snapshotted at message creation time.
    /// Stored directly to avoid cross-service lookups when rendering the conversation.
    /// </summary>
    public string SenderName { get; set; } = "";

    /// <summary>Role of the sender (e.g., <c>Customer</c>, <c>Admin</c>, <c>StoreManager</c>).</summary>
    public string SenderRole { get; set; } = "";

    /// <summary>The text content of the message.</summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Indicates whether the sender is a staff member (Admin or StoreManager).
    /// Used by the frontend to apply different styling to staff replies vs. customer messages.
    /// </summary>
    public bool IsStaff { get; set; }

    /// <summary>UTC timestamp of when the message was sent.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property back to the parent support ticket.</summary>
    public SupportTicket? Ticket { get; set; }
}
