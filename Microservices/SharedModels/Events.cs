namespace SharedModels.Events;

// Events published via HTTP callbacks between services
public record OrderPlacedEvent(Guid OrderId, Guid CustomerId, string CustomerEmail, string CustomerName, decimal TotalAmount, string DeliveryAddress, List<OrderItemEvent> Items, DateTime CreatedAt);
public record OrderItemEvent(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
public record OrderStatusChangedEvent(Guid OrderId, Guid CustomerId, string CustomerEmail, string NewStatus, DateTime ChangedAt);
public record PaymentCompletedEvent(Guid OrderId, Guid CustomerId, string CustomerEmail, decimal Amount);
