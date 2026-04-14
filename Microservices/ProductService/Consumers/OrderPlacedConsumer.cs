using MassTransit;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using SharedModels.Events;

namespace ProductService.Consumers;

/// <summary>
/// Consumes <see cref="OrderPlacedEvent"/> published by OrderService via RabbitMQ.
/// Decrements stock for each ordered product within a single database transaction.
/// Replaces the direct HTTP call previously made from OrderService to ProductService.
/// </summary>
public class OrderPlacedConsumer(
    ProductDbContext db,
    ILogger<OrderPlacedConsumer> logger) : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var evt = context.Message;
        logger.LogInformation("Received OrderPlacedEvent for Order {OrderId} — decrementing stock for {Count} items",
            evt.OrderId, evt.Items.Count);

        var productIds = evt.Items.Select(i => i.ProductId).ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        var updatedIds = new List<Guid>();

        // All stock updates in a single transaction — no partial updates
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in evt.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null)
                {
                    logger.LogWarning("OrderPlacedEvent: Product {ProductId} not found in ProductService DB, skipping",
                        item.ProductId);
                    continue;
                }

                var newStock = product.StockQuantity - item.Quantity;
                if (newStock < 0)
                {
                    logger.LogWarning("OrderPlacedEvent: Product {ProductId} stock would go below zero " +
                        "(current: {Current}, decrement: {Qty}), setting to 0",
                        item.ProductId, product.StockQuantity, item.Quantity);
                    newStock = 0;
                }

                product.StockQuantity = newStock;
                updatedIds.Add(item.ProductId);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("Stock decremented for Order {OrderId}. Updated products: [{Ids}]",
                evt.OrderId, string.Join(", ", updatedIds));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed to decrement stock for Order {OrderId}", evt.OrderId);
            throw; // rethrow so MassTransit retries
        }
    }
}
