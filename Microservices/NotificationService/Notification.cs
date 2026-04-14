namespace NotificationService.Models;

/// <summary>
/// Represents a persisted in-app notification for a specific user.
/// Notifications are stored in the database and pushed in real-time via SignalR.
/// Users can view, mark as read, and delete their notifications.
/// </summary>
public class Notification
{
    /// <summary>Unique identifier for the notification (primary key).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ID of the user this notification is addressed to.</summary>
    public Guid UserId { get; set; }

    /// <summary>Short title displayed in the notification bell dropdown (e.g., "Order Shipped").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Full notification message body with contextual details.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Visual type hint used by the frontend to apply colour coding.
    /// Valid values: <c>info</c>, <c>success</c>, <c>warning</c>, <c>error</c>, <c>order</c>.
    /// </summary>
    public string Type { get; set; } = "info"; // info | success | warning | error | order

    /// <summary>Optional deep-link URL the user is navigated to when clicking the notification.</summary>
    public string? Link { get; set; }

    /// <summary>Whether the user has read this notification. Used to compute the unread badge count.</summary>
    public bool IsRead { get; set; }

    /// <summary>UTC timestamp of when the notification was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
