using Microsoft.AspNetCore.Mvc;
using NotificationService.Services;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/v1/notifications/internal")]
public class InternalNotificationController(
    NotificationService.Services.NotificationService notif,
    EmailService email) : ControllerBase
{
    [HttpPost("user")]
    public async Task<IActionResult> SendToUser([FromBody] SendToUserRequest req)
    {
        await notif.SendToUserAsync(req.UserId, req.Title, req.Message, req.Type ?? "info", req.Link);
        return Ok();
    }

    [HttpPost("role")]
    public async Task<IActionResult> SendToRole([FromBody] SendToRoleRequest req)
    {
        await notif.SendToRoleAsync(req.Role, req.Title, req.Message, req.Type ?? "info", req.Link);
        return Ok();
    }

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

public record SendToUserRequest(Guid UserId, string Title, string Message, string? Type, string? Link);
public record SendToRoleRequest(string Role, string Title, string Message, string? Type, string? Link);
public record OrderEmailRequest(string Email, string FirstName, string OrderId, decimal TotalAmount, List<OrderItemDto> Items, string DeliveryAddress, string? EstimatedDelivery);
public record OrderStatusEmailRequest(string Email, string FirstName, string OrderId, string Status, decimal TotalAmount);
public record OrderItemDto(string ProductName, int Quantity, decimal UnitPrice);
