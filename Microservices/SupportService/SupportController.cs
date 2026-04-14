using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SupportService.Data;
using SupportService.Hubs;
using SupportService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace SupportService.Controllers;

/// <summary>
/// Manages the customer support ticket system for FreshMart.
/// Customers can create tickets and add messages to their own tickets.
/// Admin and StoreManager staff can view all tickets, reply to any ticket, and update ticket status.
/// New messages are pushed in real-time to all participants via SignalR.
/// All endpoints require authentication.
/// </summary>
[ApiController]
[Route("api/v1/support")]
[Authorize]
public class SupportController(SupportDbContext db, IHubContext<SupportHub> hub) : ControllerBase
{
    /// <summary>Extracts the authenticated user's ID from the JWT <c>sub</c> claim.</summary>
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID not found"));

    /// <summary>Returns true if the authenticated user has a staff role (Admin or StoreManager).</summary>
    private bool IsStaff => User.IsInRole("Admin") || User.IsInRole("StoreManager");

    /// <summary>Maps a <see cref="SupportTicket"/> entity to an anonymous DTO for API responses.</summary>
    private static object TicketDto(SupportTicket t) => new
    {
        id = t.Id,
        customerId = t.CustomerId,
        customerName = t.CustomerName,
        customerEmail = t.CustomerEmail,
        subject = t.Subject,
        category = t.Category,
        status = t.Status,
        priority = t.Priority,
        createdAt = t.CreatedAt.ToString("o"),
        updatedAt = t.UpdatedAt.ToString("o"),
        resolvedAt = t.ResolvedAt?.ToString("o"),
        messageCount = t.Messages?.Count ?? 0
    };

    /// <summary>Maps a <see cref="SupportMessage"/> entity to an anonymous DTO for API responses.</summary>
    private static object MessageDto(SupportMessage m) => new
    {
        id = m.Id,
        ticketId = m.TicketId,
        senderId = m.SenderId,
        senderName = m.SenderName,
        senderRole = m.SenderRole,
        message = m.Message,
        isStaff = m.IsStaff,
        createdAt = m.CreatedAt.ToString("o")
    };

    /// <summary>
    /// Creates a new support ticket with an initial message.
    /// Reads the customer's name and email from JWT claims to avoid a cross-service lookup.
    /// Upserts a local AppUser projection for future message sender resolution.
    /// Returns 201 Created with the new ticket details.
    /// Accessible by: authenticated users (customers).
    /// </summary>
    // POST /api/v1/support/tickets
    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest req)
    {
        // Read user info from JWT claims — no local DB lookup needed
        var firstName = User.FindFirstValue(JwtRegisteredClaimNames.GivenName)
            ?? User.FindFirstValue(ClaimTypes.GivenName) ?? "User";
        var lastName = User.FindFirstValue(JwtRegisteredClaimNames.FamilyName)
            ?? User.FindFirstValue(ClaimTypes.Surname) ?? "";
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? User.FindFirstValue(ClaimTypes.Email) ?? "";
        var fullName = $"{firstName} {lastName}".Trim();

        // Upsert local AppUser projection so AddMessage can find the user
        var appUser = await db.Users.FindAsync(UserId);
        if (appUser == null)
            db.Users.Add(new AppUser { Id = UserId, Email = email, FirstName = firstName, LastName = lastName });
        else
        {
            appUser.Email = email;
            appUser.FirstName = firstName;
            appUser.LastName = lastName;
        }

        var ticket = new SupportTicket
        {
            CustomerId = UserId,
            CustomerName = fullName,
            CustomerEmail = email,
            Subject = req.Subject,
            Category = req.Category,
            Priority = req.Priority ?? "Medium"
        };
        db.SupportTickets.Add(ticket);

        var msg = new SupportMessage
        {
            TicketId = ticket.Id,
            SenderId = UserId,
            SenderName = fullName,
            SenderRole = "Customer",
            Message = req.Description,
            IsStaff = false
        };
        db.SupportMessages.Add(msg);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, TicketDto(ticket));
    }

    /// <summary>
    /// Returns a filtered list of support tickets.
    /// Customers see only their own tickets. Staff (Admin/StoreManager) see all tickets.
    /// Supports optional filtering by status, priority, and category.
    /// Results are ordered by creation date descending.
    /// Accessible by: all authenticated users (scoped by role).
    /// </summary>
    // GET /api/v1/support/tickets
    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets([FromQuery] string? status, [FromQuery] string? priority, [FromQuery] string? category)
    {
        var q = db.SupportTickets.Include(t => t.Messages).AsQueryable();
        if (!IsStaff) q = q.Where(t => t.CustomerId == UserId);
        if (!string.IsNullOrEmpty(status)) q = q.Where(t => t.Status == status);
        if (!string.IsNullOrEmpty(priority)) q = q.Where(t => t.Priority == priority);
        if (!string.IsNullOrEmpty(category)) q = q.Where(t => t.Category == category);

        var tickets = await q.OrderByDescending(t => t.CreatedAt).Select(t => TicketDto(t)).ToListAsync();
        return Ok(tickets);
    }

    /// <summary>
    /// Returns the full details of a single support ticket including its message thread.
    /// Customers can only access their own tickets. Staff can access any ticket.
    /// Returns 403 Forbidden if a customer tries to access another customer's ticket.
    /// Accessible by: all authenticated users (with ownership enforcement for customers).
    /// </summary>
    // GET /api/v1/support/tickets/{id}
    [HttpGet("tickets/{id}")]
    public async Task<IActionResult> GetTicket(Guid id)
    {
        var ticket = await db.SupportTickets.Include(t => t.Messages).FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null) return NotFound();
        if (!IsStaff && ticket.CustomerId != UserId) return Forbid();

        return Ok(new
        {
            ticket = TicketDto(ticket),
            messages = ticket.Messages.OrderBy(m => m.CreatedAt).Select(m => MessageDto(m))
        });
    }

    /// <summary>
    /// Adds a new message to a support ticket's conversation thread.
    /// Automatically transitions the ticket from <c>Open</c> to <c>InProgress</c> when staff first replies.
    /// Pushes the new message in real-time to all clients subscribed to the ticket's SignalR group.
    /// Returns 403 Forbidden if a customer tries to message a ticket they don't own.
    /// Accessible by: all authenticated users (with ownership enforcement for customers).
    /// </summary>
    // POST /api/v1/support/tickets/{id}/messages
    [HttpPost("tickets/{id}/messages")]
    public async Task<IActionResult> AddMessage(Guid id, [FromBody] AddMessageRequest req)
    {
        var ticket = await db.SupportTickets.FindAsync(id);
        if (ticket == null) return NotFound();
        if (!IsStaff && ticket.CustomerId != UserId) return Forbid();

        // Read sender info from JWT claims — fallback to local projection
        var firstName = User.FindFirstValue(JwtRegisteredClaimNames.GivenName)
            ?? User.FindFirstValue(ClaimTypes.GivenName) ?? "";
        var lastName = User.FindFirstValue(JwtRegisteredClaimNames.FamilyName)
            ?? User.FindFirstValue(ClaimTypes.Surname) ?? "";
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? User.FindFirstValue(ClaimTypes.Email) ?? "";
        var senderName = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrEmpty(senderName))
        {
            var appUser = await db.Users.FindAsync(UserId);
            senderName = appUser != null ? $"{appUser.FirstName} {appUser.LastName}".Trim() : "User";
        }

        var role = User.FindFirstValue(ClaimTypes.Role)
            ?? User.FindFirstValue("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            ?? "Customer";

        var msg = new SupportMessage
        {
            TicketId = id,
            SenderId = UserId,
            SenderName = senderName,
            SenderRole = role,
            Message = req.Message,
            IsStaff = IsStaff
        };
        db.SupportMessages.Add(msg);

        ticket.UpdatedAt = DateTime.UtcNow;
        if (IsStaff && ticket.Status == "Open")
            ticket.Status = "InProgress";

        await db.SaveChangesAsync();

        var payload = MessageDto(msg);
        await hub.Clients.Group($"ticket:{id}").SendAsync("newMessage", payload);

        return Ok(payload);
    }

    /// <summary>
    /// Updates the status and optionally the priority of a support ticket.
    /// Sets <c>ResolvedAt</c> when the status is changed to <c>Resolved</c> or <c>Closed</c>.
    /// Pushes a <c>ticketUpdated</c> event to all clients subscribed to the ticket's SignalR group.
    /// Accessible by: Admin, StoreManager.
    /// </summary>
    // PATCH /api/v1/support/tickets/{id}/status
    [HttpPatch("tickets/{id}/status")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTicketStatusRequest req)
    {
        var ticket = await db.SupportTickets.FindAsync(id);
        if (ticket == null) return NotFound();

        ticket.Status = req.Status;
        ticket.UpdatedAt = DateTime.UtcNow;
        if (req.Status == "Resolved" || req.Status == "Closed")
            ticket.ResolvedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(req.Priority))
            ticket.Priority = req.Priority;

        await db.SaveChangesAsync();

        await hub.Clients.Group($"ticket:{id}").SendAsync("ticketUpdated", new
        {
            id = ticket.Id,
            status = ticket.Status,
            priority = ticket.Priority,
            updatedAt = ticket.UpdatedAt.ToString("o")
        });

        return NoContent();
    }
}

/// <summary>Request body for creating a new support ticket.</summary>
/// <param name="Subject">Brief description of the issue (used as the ticket title).</param>
/// <param name="Category">Issue category: <c>Order</c>, <c>Payment</c>, <c>Delivery</c>, <c>Product</c>, or <c>Other</c>.</param>
/// <param name="Description">Detailed description of the issue, sent as the first message in the thread.</param>
/// <param name="Priority">Optional priority: <c>Low</c>, <c>Medium</c>, or <c>High</c>. Defaults to <c>Medium</c>.</param>
public record CreateTicketRequest(string Subject, string Category, string Description, string? Priority);

/// <summary>Request body for adding a new message to a support ticket thread.</summary>
/// <param name="Message">The text content of the message.</param>
public record AddMessageRequest(string Message);

/// <summary>Request body for updating a support ticket's status and optionally its priority.</summary>
/// <param name="Status">New status: <c>Open</c>, <c>InProgress</c>, <c>Resolved</c>, or <c>Closed</c>.</param>
/// <param name="Priority">Optional new priority: <c>Low</c>, <c>Medium</c>, or <c>High</c>.</param>
public record UpdateTicketStatusRequest(string Status, string? Priority);
