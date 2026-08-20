namespace OrderService.Core.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }

    // saving name and email at order time so it doesnt change if user edits their profile later
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerFirstName { get; set; } = string.Empty;

    /// <summary>Order status flow: Pending -> Processing -> Shipped -> Delivered</summary>
    public string Status { get; set; } = "Pending";

    public decimal SubTotal { get; set; }

    public decimal DeliveryFee { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string DeliveryAddress { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EstimatedDelivery { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }

    // product name saved here so order history works even if product is deleted
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
