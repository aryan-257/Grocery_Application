namespace ProductService.Models;

/// <summary>
/// Local read-model projection of an order, used by ProductService to verify
/// whether a customer has purchased a product before allowing them to review it.
/// This avoids a synchronous cross-service call to OrderService at review time.
/// Not a full order record — only the fields needed for purchase verification are stored.
/// </summary>
public class OrderProjection
{
    /// <summary>Order ID, matching the ID in OrderService.</summary>
    public Guid Id { get; set; }

    /// <summary>ID of the customer who placed the order. Used to scope purchase checks per user.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Current order status. Orders with status <c>Cancelled</c> are excluded from
    /// purchase verification so customers cannot review products from cancelled orders.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The line items in this order. Used to check if a specific product was purchased.</summary>
    public ICollection<OrderItemProjection> Items { get; set; } = [];
}

/// <summary>
/// Represents a single line item within an <see cref="OrderProjection"/>.
/// Only the product ID is needed for the can-review eligibility check.
/// </summary>
public class OrderItemProjection
{
    /// <summary>Unique identifier for this order item projection.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the parent <see cref="OrderProjection"/>.</summary>
    public Guid OrderId { get; set; }

    /// <summary>ID of the product in this line item. Matched against the product being reviewed.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Navigation property back to the parent order projection.</summary>
    public OrderProjection Order { get; set; } = null!;
}
