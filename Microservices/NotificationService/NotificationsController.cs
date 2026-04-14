using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NotificationService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Controllers;

/// <summary>
/// Provides the authenticated user with access to their own notification inbox.
/// Supports listing, marking as read, and deleting notifications.
/// All endpoints are scoped to the authenticated user — users cannot access other users' notifications.
/// </summary>
[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController(NotificationDbContext db) : ControllerBase
{
    /// <summary>Extracts the authenticated user's ID from the JWT <c>sub</c> claim.</summary>
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Returns the 50 most recent notifications for the authenticated user, ordered by newest first.
    /// Used to populate the notification bell dropdown in the frontend navbar.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await db.Notifications
            .Where(n => n.UserId == UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new {
                id = n.Id.ToString(),
                title = n.Title,
                message = n.Message,
                type = n.Type,
                link = n.Link,
                isRead = n.IsRead,
                createdAt = n.CreatedAt.ToString("o")
            })
            .ToListAsync();
        return Ok(items);
    }

    /// <summary>
    /// Returns the count of unread notifications for the authenticated user.
    /// Used to display the unread badge number on the notification bell icon.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await db.Notifications.CountAsync(n => n.UserId == UserId && !n.IsRead);
        return Ok(new { count });
    }

    /// <summary>
    /// Marks a single notification as read.
    /// Returns 404 if the notification does not exist or does not belong to the authenticated user.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId);
        if (n == null) return NotFound();
        n.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Marks all of the authenticated user's unread notifications as read in a single bulk operation.
    /// Returns 204 No Content on success.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await db.Notifications
            .Where(n => n.UserId == UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return NoContent();
    }

    /// <summary>
    /// Permanently deletes a single notification.
    /// Returns 404 if the notification does not exist or does not belong to the authenticated user.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId);
        if (n == null) return NotFound();
        db.Notifications.Remove(n);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Permanently deletes all notifications for the authenticated user.
    /// Returns 204 No Content on success.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAll()
    {
        await db.Notifications.Where(n => n.UserId == UserId).ExecuteDeleteAsync();
        return NoContent();
    }
}
