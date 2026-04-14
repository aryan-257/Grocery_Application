using Microsoft.AspNetCore.Mvc;
using NotificationService.Services;

namespace NotificationService.Controllers;

/// <summary>
/// Internal-only controller for receiving notification and email dispatch requests from other microservices.
/// Not exposed to the public API — intended for server-to-server communication within the Docker network.
/// No authentication is required because this endpoint is only reachable from within the service mesh.
/// </summary>
[ApiController]
[Route("api/v1/notifications/internal")]
public class InternalNotificationController(
    NotificationService.Services.NotificationService notif,
    EmailService email) : ControllerBase
{
    /// <summary>
    /// Sends a real-time in-app notification to a specific user.
    /// Persists the notification and pushes it via SignalR to the user's connected clients.
    /// Called by OrderService and other services after significant events.
    /// </summary>
    [HttpPost("user")]
    public async Task<IActionResult> SendToUser([FromBody] SendToUserRequest req)
    {
        await notif.SendToUserAsync(req.UserId, req.Title, req.Message, req.Type ?? "info", req.Link);
        return Ok();
    }

    /// <summary>
    /// Broadcasts a real-time in-app notification to all connected users with a specific role.
    /// Role broadcasts are live-only (not persisted). Used for Admin/StoreManager new-order alerts.
    /// </summary>
    [HttpPost("role")]
    public async Task<IActionResult> SendToRole([FromBody] SendToRoleRequest req)
    {
        await notif.SendToRoleAsync(req.Role, req.Title, req.Message, req.Type ?? "info", req.Link);
        return Ok();
    }

    /// <summary>
    /// Sends an order confirmation email to the customer after successful payment.
    /// Includes the full order items table, total, delivery address, and estimated delivery date.
    /// Called by OrderService after the <c>complete-payment</c> endpoint is invoked.
    /// </summary>
    [HttpPost("email/order-placed")]
    public async Task<IActionResult> SendOrderPlacedEmail([FromBody] OrderEmailRequest req)
    {
        if (!Guid.TryParse(req.OrderId, out var orderId))
            return BadRequest(new { error = "Invalid OrderId" });
        var order = new OrderInfo(
            orderId,
            req.TotalAmount,
            req.Items.Select(i => new OrderItemInfo(i.ProductName, i.Quantity, i.UnitPrice)),
            req.EstimatedDelivery != null ? DateTime.Parse(req.EstimatedDelivery) : null
        );
        await email.SendOrderPlacedAsync(req.Email, req.FirstName, order);
        return Ok();
    }

    /// <summary>
    /// Sends a transactional order status update email to the customer.
    /// Routes to the appropriate email template based on the new status value.
    /// Supported statuses: <c>Processing</c>, <c>Shipped</c>, <c>OutForDelivery</c>, <c>Delivered</c>, <c>Cancelled</c>.
    /// Called by OrderService when an Admin, StoreManager, or DeliveryDriver updates an order's status.
    /// </summary>
    [HttpPost("email/order-status")]
    public async Task<IActionResult> SendOrderStatusEmail([FromBody] OrderStatusEmailRequest req)
    {
        if (!Guid.TryParse(req.OrderId, out var orderId))
            return BadRequest(new { error = "Invalid OrderId" });
        var order = new OrderInfo(orderId, req.TotalAmount, new List<OrderItemInfo>());
        var task = req.Status switch
        {
            "Processing"     => email.SendOrderProcessingAsync(req.Email, req.FirstName, order),
            "Shipped"        => email.SendOrderShippedAsync(req.Email, req.FirstName, order),
            "OutForDelivery" => email.SendOutForDeliveryAsync(req.Email, req.FirstName, order),
            "Delivered"      => email.SendOrderDeliveredAsync(req.Email, req.FirstName, order),
            "Cancelled"      => email.SendOrderCancelledAsync(req.Email, req.FirstName, order),
            _                => Task.CompletedTask
        };
        await task;
        return Ok();
    }
}

/// <summary>Request body for sending a targeted in-app notification to a specific user.</summary>
public record SendToUserRequest(Guid UserId, string Title, string Message, string? Type, string? Link);

/// <summary>Request body for broadcasting an in-app notification to all users of a specific role.</summary>
public record SendToRoleRequest(string Role, string Title, string Message, string? Type, string? Link);

/// <summary>Request body for sending an order confirmation email with full item details.</summary>
public record OrderEmailRequest(string Email, string FirstName, string OrderId, decimal TotalAmount, List<OrderItemDto> Items, string DeliveryAddress, string? EstimatedDelivery);

/// <summary>Request body for sending an order status update email.</summary>
public record OrderStatusEmailRequest(string Email, string FirstName, string OrderId, string Status, decimal TotalAmount);

/// <summary>Represents a single order line item in an email request payload.</summary>
public record OrderItemDto(string ProductName, int Quantity, decimal UnitPrice);
