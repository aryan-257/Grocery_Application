namespace ProductService.Models;

/// <summary>
/// Local order projection for purchase verification (can-review check).
/// Not a cross-service dependency — populated from order service events or read-through.
/// </summary>
public class OrderProjection
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public ICollection<OrderItemProjection> Items { get; set; } = [];
}

public class OrderItemProjection
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public OrderProjection Order { get; set; } = null!;
}
