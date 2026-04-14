using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs;

/// <summary>
/// SignalR hub for real-time notification delivery.
/// On connection, each client is automatically added to:
/// - A user-specific group (<c>user:{userId}</c>) for targeted notifications.
/// - A role-based group (<c>role:{role}</c>) for broadcast notifications to all users of a role.
/// Requires a valid JWT — unauthenticated connections are rejected.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    /// <summary>
    /// Called when a client establishes a SignalR connection.
    /// Adds the connection to the user's personal group and their role group,
    /// enabling both targeted and role-broadcast notification delivery.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = Context.User?.FindFirstValue(ClaimTypes.Role)
                ?? Context.User?.FindFirstValue("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");

        if (userId != null)
        {
            // Each user joins their own group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }
        if (role != null)
        {
            // Role-based groups for broadcast notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");
        }
        await base.OnConnectedAsync();
    }
}
