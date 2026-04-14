using MassTransit;
using NotificationService.Services;
using SharedModels.Events;

namespace NotificationService.Consumers;

/// <summary>
/// Consumes <see cref="OrderPlacedEvent"/> published by OrderService via RabbitMQ.
/// Sends in-app notifications to the customer, Admin, and StoreManager roles,
/// and sends an order confirmation email. Replaces direct HTTP calls from OrderService.
/// </summary>
public class OrderPlacedConsumer(
    NotificationService.Services.NotificationService notifService,
    EmailService emailService,
    ILogger<OrderPlacedConsumer> logger) : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var evt = context.Message;
        var shortId = evt.OrderId.ToString()[..8].ToUpper();
        logger.LogInformation("Received OrderPlacedEvent for Order {OrderId}", evt.OrderId);

        try
        {
            await notifService.SendToUserAsync(evt.CustomerId,
                "Payment Successful",
                $"Payment for order #{shortId} completed. Total: Rs.{evt.TotalAmount:F2}",
                "success", $"/orders/{evt.OrderId}/track");

            await notifService.SendToRoleAsync("Admin", "New Order Received",
                $"Order #{shortId} placed for Rs.{evt.TotalAmount:F2}", "order", "/admin/orders");
            await notifService.SendToRoleAsync("StoreManager", "New Order Received",
                $"Order #{shortId} placed for Rs.{evt.TotalAmount:F2}", "order", "/admin/orders");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send in-app notifications for Order {OrderId}", evt.OrderId);
        }

        if (string.IsNullOrEmpty(evt.CustomerEmail))
        {
            logger.LogWarning("OrderPlacedEvent: CustomerEmail empty for Order {OrderId}, skipping email", evt.OrderId);
            return;
        }

        try
        {
            var items = evt.Items.Select(i => new OrderItemInfo(i.ProductName, i.Quantity, i.UnitPrice));
            var orderInfo = new OrderInfo(
                Id: evt.OrderId,
                TotalAmount: evt.TotalAmount,
                Items: items,
                EstimatedDelivery: evt.CreatedAt.AddDays(2));

            await emailService.SendOrderPlacedAsync(evt.CustomerEmail, evt.CustomerName, orderInfo);
            logger.LogInformation("Order confirmation email sent for Order {OrderId}", evt.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send confirmation email for Order {OrderId}", evt.OrderId);
        }
    }
}
