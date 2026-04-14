using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using SharedModels.Events;

namespace OrderService.Messaging;

/// <summary>
/// Consumes <see cref="PaymentCompletedEvent"/> published by PaymentService via RabbitMQ.
/// Automatically transitions the corresponding order status to "Processing"
/// so the fulfilment workflow starts without manual intervention.
/// Idempotent: skips update if order is already Processing or beyond.
/// </summary>
public class PaymentCompletedConsumer(OrderDbContext db, ILogger<PaymentCompletedConsumer> logger)
    : IConsumer<PaymentCompletedEvent>
{
    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Received PaymentCompletedEvent for Order {OrderId}", evt.OrderId);

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId);
        if (order == null)
        {
            logger.LogWarning("PaymentCompletedEvent: Order {OrderId} not found", evt.OrderId);
            return;
        }

        // Idempotency: skip if already past Pending
        var skipStatuses = new[] { "Processing", "Shipped", "OutForDelivery", "Delivered", "Cancelled" };
        if (skipStatuses.Contains(order.Status))
        {
            logger.LogDebug("PaymentCompletedEvent: Order {OrderId} already at status {Status}, skipping",
                evt.OrderId, order.Status);
            return;
        }

        order.Status = "Processing";
        await db.SaveChangesAsync();

        logger.LogInformation("Order {OrderId} transitioned to Processing via PaymentCompletedEvent", evt.OrderId);
    }
}
