using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using SharedModels.Events;

namespace OrderService.Messaging;

/// <summary>
/// Consumes <see cref="PaymentCompletedEvent"/> published by PaymentService via RabbitMQ.
/// Automatically transitions the corresponding order status to "Processing"
/// and publishes <see cref="OrderPlacedEvent"/> so NotificationService sends
/// the confirmation email and in-app notification — guaranteeing delivery even
/// when the frontend complete-payment call is never made.
/// Idempotent: skips update if order is already Processing or beyond.
/// </summary>
public class PaymentCompletedConsumer(
    OrderDbContext db,
    IPublishEndpoint publishEndpoint,
    ILogger<PaymentCompletedConsumer> logger)
    : IConsumer<PaymentCompletedEvent>
{
    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Received PaymentCompletedEvent for Order {OrderId}", evt.OrderId);

        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == evt.OrderId);
        if (order == null)
        {
            logger.LogWarning("PaymentCompletedEvent: Order {OrderId} not found", evt.OrderId);
            return;
        }

        // Idempotency: skip if already past Pending
        var skipStatuses = new[] { "Processing", "Shipped", "OutForDelivery", "Delivered", "Cancelled" };
        bool alreadyProcessed = skipStatuses.Contains(order.Status);

        if (!alreadyProcessed)
        {
            order.Status = "Processing";
            await db.SaveChangesAsync();
            logger.LogInformation("Order {OrderId} transitioned to Processing via PaymentCompletedEvent", evt.OrderId);
        }
        else
        {
            logger.LogDebug("PaymentCompletedEvent: Order {OrderId} already at status {Status}, skipping status update",
                evt.OrderId, order.Status);
        }

        // Always publish OrderPlacedEvent so notifications/email fire reliably.
        // This covers the case where the frontend never calls complete-payment.
        try
        {
            // Resolve customer info — use snapshot on order, fall back to local AppUser projection
            var email = order.CustomerEmail;
            var firstName = order.CustomerFirstName;
            if (string.IsNullOrEmpty(email))
            {
                var appUser = await db.Users.FindAsync(order.CustomerId);
                email = appUser?.Email ?? evt.CustomerEmail ?? "";
                firstName = appUser?.FirstName ?? "Customer";
            }

            await publishEndpoint.Publish(new OrderPlacedEvent(
                OrderId: order.Id,
                CustomerId: order.CustomerId,
                CustomerEmail: email,
                CustomerName: firstName,
                TotalAmount: order.TotalAmount,
                DeliveryAddress: order.DeliveryAddress,
                Items: order.Items.Select(i => new OrderItemEvent(
                    i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
                CreatedAt: order.CreatedAt));

            logger.LogInformation("Published OrderPlacedEvent for Order {OrderId} via PaymentCompletedConsumer", order.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish OrderPlacedEvent for Order {OrderId}", evt.OrderId);
        }
    }
}
