using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SupportService.Hubs;

/// <summary>
/// SignalR hub for real-time support ticket messaging.
/// Clients join ticket-specific groups to receive live message updates as they are posted.
/// On connection, each client is also added to user and role groups for targeted notifications.
/// Requires a valid JWT — unauthenticated connections are rejected.
/// </summary>
[Authorize]
public class SupportHub : Hub
{
    /// <summary>
    /// Adds the caller's connection to the SignalR group for a specific support ticket.
    /// Called by the frontend when the user opens a ticket's conversation view.
    /// Enables real-time delivery of new messages posted to that ticket.
    /// </summary>
    /// <param name="ticketId">The ID of the support ticket to subscribe to.</param>
    public async Task JoinTicket(string ticketId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket:{ticketId}");
    }

    /// <summary>
    /// Removes the caller's connection from the SignalR group for a specific support ticket.
    /// Called by the frontend when the user navigates away from the ticket conversation view.
    /// </summary>
    /// <param name="ticketId">The ID of the support ticket to unsubscribe from.</param>
    public async Task LeaveTicket(string ticketId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket:{ticketId}");
    }

    /// <summary>
    /// Called when a client establishes a SignalR connection.
    /// Adds the connection to the user's personal group and their role group,
    /// enabling targeted and role-based notification delivery alongside ticket messaging.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = Context.User?.FindFirstValue(ClaimTypes.Role) ?? "";

        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        if (!string.IsNullOrEmpty(role))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");

        await base.OnConnectedAsync();
    }
}
