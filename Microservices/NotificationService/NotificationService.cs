using NotificationService.Data;
using NotificationService.Hubs;
using NotificationService.Models;
using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Services;

/// <summary>
/// Core service for creating and delivering in-app notifications.
/// Persists user-targeted notifications to the database and pushes them
/// in real-time to connected clients via SignalR.
/// Role broadcasts are live-only (not persisted) to avoid fan-out storage costs.
/// </summary>
public class NotificationService(NotificationDbContext db, IHubContext<NotificationHub> hub)
{
    /// <summary>
    /// Creates a notification for a specific user, persists it to the database,
    /// and pushes it in real-time to all of the user's connected SignalR clients.
    /// The notification will appear in the user's inbox even if they are not currently connected.
    /// </summary>
    /// <param name="userId">Target user's ID.</param>
    /// <param name="title">Short notification title.</param>
    /// <param name="message">Full notification message body.</param>
    /// <param name="type">Visual type: <c>info</c>, <c>success</c>, <c>warning</c>, <c>error</c>, or <c>order</c>.</param>
    /// <param name="link">Optional deep-link URL for navigation on click.</param>
    public async Task SendToUserAsync(Guid userId, string title, string message, string type = "info", string? link = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Link = link
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var payload = ToPayload(notification);
        await hub.Clients.Group($"user:{userId}").SendAsync("notification", payload);
    }

    /// <summary>
    /// Broadcasts a live notification to all currently connected users with the specified role.
    /// Role broadcasts are NOT persisted — they are ephemeral SignalR pushes only.
    /// Users who are offline will not receive the notification.
    /// Used for Admin/StoreManager new-order alerts and DeliveryDriver pickup notifications.
    /// </summary>
    /// <param name="role">Target role (e.g., <c>Admin</c>, <c>StoreManager</c>, <c>DeliveryDriver</c>).</param>
    /// <param name="title">Short notification title.</param>
    /// <param name="message">Full notification message body.</param>
    /// <param name="type">Visual type hint.</param>
    /// <param name="link">Optional deep-link URL.</param>
    public async Task SendToRoleAsync(string role, string title, string message, string type = "info", string? link = null)
    {
        // For role broadcasts we don't persist per-user — just push live
        var payload = new
        {
            id = Guid.NewGuid().ToString(),
            title,
            message,
            type,
            link,
            isRead = false,
            createdAt = DateTime.UtcNow.ToString("o")
        };
        await hub.Clients.Group($"role:{role}").SendAsync("notification", payload);
    }

    /// <summary>
    /// Maps a <see cref="Notification"/> entity to an anonymous object suitable for SignalR delivery.
    /// </summary>
    private static object ToPayload(Notification n) => new
    {
        id = n.Id.ToString(),
        title = n.Title,
        message = n.Message,
        type = n.Type,
        link = n.Link,
        isRead = n.IsRead,
        createdAt = n.CreatedAt.ToString("o")
    };
}
