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

[ApiController]
[Route("api/v1/support")]
[Authorize]
public class SupportController(SupportDbContext db, IHubContext<SupportHub> hub) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID not found"));

    private bool IsStaff => User.IsInRole("Admin") || User.IsInRole("StoreManager");

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

    // POST /api/v1/support/tickets
    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest req)
    {
        var user = await db.Users.FindAsync(UserId);
        if (user == null) return Unauthorized();

        var ticket = new SupportTicket
        {
            CustomerId = UserId,
            CustomerName = $"{user.FirstName} {user.LastName}",
            CustomerEmail = user.Email,
            Subject = req.Subject,
            Category = req.Category,
            Priority = req.Priority ?? "Medium"
        };
        db.SupportTickets.Add(ticket);

        var msg = new SupportMessage
        {
            TicketId = ticket.Id,
            SenderId = UserId,
            SenderName = $"{user.FirstName} {user.LastName}",
            SenderRole = "Customer",
            Message = req.Description,
            IsStaff = false
        };
        db.SupportMessages.Add(msg);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, TicketDto(ticket));
    }

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

    // POST /api/v1/support/tickets/{id}/messages
    [HttpPost("tickets/{id}/messages")]
    public async Task<IActionResult> AddMessage(Guid id, [FromBody] AddMessageRequest req)
    {
        var ticket = await db.SupportTickets.FindAsync(id);
        if (ticket == null) return NotFound();
        if (!IsStaff && ticket.CustomerId != UserId) return Forbid();

        var user = await db.Users.FindAsync(UserId);
        if (user == null) return Unauthorized();

        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Customer";
        var msg = new SupportMessage
        {
            TicketId = id,
            SenderId = UserId,
            SenderName = $"{user.FirstName} {user.LastName}",
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

public record CreateTicketRequest(string Subject, string Category, string Description, string? Priority);
public record AddMessageRequest(string Message);
public record UpdateTicketStatusRequest(string Status, string? Priority);
