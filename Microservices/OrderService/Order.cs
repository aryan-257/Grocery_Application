namespace OrderService.Models;

/// <summary>
/// Represents a customer order in the FreshMart platform.
/// Captures the full financial breakdown, delivery details, and lifecycle status of an order.
/// Created when a customer checks out their cart and payment is initiated.
/// </summary>
public class Order
{
    /// <summary>Unique identifier for the order (primary key).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ID of the customer who placed the order. Used for ownership checks and notifications.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Snapshotted customer email at order creation time.
    /// Stored directly so status emails can be sent even if the user's profile changes later.
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Snapshotted customer first name at order creation time.
    /// Used to personalise transactional emails without a cross-service lookup.
    /// </summary>
    public string CustomerFirstName { get; set; } = string.Empty;

    /// <summary>
    /// Current lifecycle status of the order.
    /// Valid values: <c>Pending</c>, <c>Processing</c>, <c>Shipped</c>,
    /// <c>OutForDelivery</c>, <c>Delivered</c>, <c>Cancelled</c>.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Sum of all line item prices before delivery fee, tax, and discounts.</summary>
    public decimal SubTotal { get; set; }

    /// <summary>Delivery fee charged for the order. Free (0) for orders over Rs.500.</summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>GST/tax amount calculated as 5% of the subtotal.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Final amount charged to the customer: SubTotal + DeliveryFee + TaxAmount - DiscountAmount.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Discount applied via a coupon code. Zero if no coupon was used.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Full delivery address provided by the customer at checkout.</summary>
    public string DeliveryAddress { get; set; } = string.Empty;

    /// <summary>Optional delivery instructions or special notes from the customer.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC timestamp of when the order was placed.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Estimated UTC delivery date, typically set to 2 days after order creation.</summary>
    public DateTime? EstimatedDelivery { get; set; }

    /// <summary>UTC timestamp of when the order was marked as Delivered. Null until delivery is confirmed.</summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>The individual product line items included in this order.</summary>
    public ICollection<OrderItem> Items { get; set; } = [];
}

/// <summary>
/// Represents a single product line item within an <see cref="Order"/>.
/// Prices are snapshotted at order creation time to preserve the historical record.
/// </summary>
public class OrderItem
{
    /// <summary>Unique identifier for this order item.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Foreign key to the parent <see cref="Order"/>.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Navigation property back to the parent order.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>ID of the product that was ordered. Used for review eligibility checks.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Product name snapshotted at order time. Preserved even if the product is later renamed.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Number of units of this product ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Price per unit at the time of ordering (after any discount). Snapshotted for historical accuracy.</summary>
    public decimal UnitPrice { get; set; }
}
