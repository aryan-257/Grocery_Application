using MassTransit;
using NotificationService.Services;
using SharedModels.Events;

namespace NotificationService.Consumers;

/// <summary>
/// Consumes <see cref="OrderStatusChangedEvent"/> published by OrderService via RabbitMQ.
/// Sends the appropriate in-app notification and transactional email for each status transition.
/// Also notifies DeliveryDriver role when an order is ready for pickup.
/// Replaces direct HTTP calls from OrderService UpdateStatus endpoint.
/// </summary>
public class OrderStatusChangedConsumer(
    NotificationService.Services.NotificationService notifService,
    EmailService emailService,
    ILogger<OrderStatusChangedConsumer> logger) : IConsumer<OrderStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedEvent> context)
    {
        var evt = context.Message;
        var shortId = evt.OrderId.ToString()[..8].ToUpper();
        logger.LogInformation("Received OrderStatusChangedEvent for Order {OrderId} → {Status}",
            evt.OrderId, evt.NewStatus);

        var (title, msg, type) = evt.NewStatus switch
        {
            "Processing"     => ("Order Processing",  $"Your order #{shortId} is being prepared.", "info"),
            "Shipped"        => ("Order Shipped",     $"Your order #{shortId} has been shipped!", "info"),
            "OutForDelivery" => ("Out for Delivery",  $"Your order #{shortId} is out for delivery!", "warning"),
            "Delivered"      => ("Order Delivered",   $"Your order #{shortId} has been delivered. Enjoy!", "success"),
            "Cancelled"      => ("Order Cancelled",   $"Your order #{shortId} has been cancelled.", "error"),
            _                => ("Order Updated",     $"Your order #{shortId} status: {evt.NewStatus}", "info")
        };

        try
        {
            await notifService.SendToUserAsync(evt.CustomerId, title, msg, type,
                $"/orders/{evt.OrderId}/track");

            if (evt.NewStatus is "Shipped" or "Processing")
                await notifService.SendToRoleAsync("DeliveryDriver", "New Delivery Available",
                    $"Order #{shortId} is ready for pickup.", "order", "/delivery");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send in-app notification for Order {OrderId}", evt.OrderId);
        }

        // Send status email
        if (string.IsNullOrEmpty(evt.CustomerEmail))
        {
            logger.LogWarning("OrderStatusChangedEvent: CustomerEmail empty for Order {OrderId} status {Status}",
                evt.OrderId, evt.NewStatus);
            return;
        }

        var emailStatuses = new[] { "Processing", "Shipped", "OutForDelivery", "Delivered", "Cancelled" };
        if (!emailStatuses.Contains(evt.NewStatus)) return;

        try
        {
            var orderInfo = new OrderInfo(
                Id: evt.OrderId,
                TotalAmount: evt.TotalAmount,
                Items: Enumerable.Empty<OrderItemInfo>());

            Task emailTask = evt.NewStatus switch
            {
                "Processing"     => emailService.SendOrderProcessingAsync(evt.CustomerEmail, evt.CustomerName, orderInfo),
                "Shipped"        => emailService.SendOrderShippedAsync(evt.CustomerEmail, evt.CustomerName, orderInfo),
                "OutForDelivery" => emailService.SendOutForDeliveryAsync(evt.CustomerEmail, evt.CustomerName, orderInfo),
                "Delivered"      => emailService.SendOrderDeliveredAsync(evt.CustomerEmail, evt.CustomerName, orderInfo),
                "Cancelled"      => emailService.SendOrderCancelledAsync(evt.CustomerEmail, evt.CustomerName, orderInfo),
                _                => Task.CompletedTask
            };
            await emailTask;
            logger.LogInformation("Status email sent for Order {OrderId} → {Status}", evt.OrderId, evt.NewStatus);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send status email for Order {OrderId}", evt.OrderId);
        }
    }
}
