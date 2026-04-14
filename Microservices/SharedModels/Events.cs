namespace SharedModels.Events;

/// <summary>
/// Published by OrderService when a customer successfully completes payment.
/// Consumed by NotificationService (send confirmation email + in-app notification)
/// and ProductService (decrement stock for each ordered item).
/// </summary>
public record OrderPlacedEvent(
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    decimal TotalAmount,
    string DeliveryAddress,
    List<OrderItemEvent> Items,
    DateTime CreatedAt);

/// <summary>A single line item within an <see cref="OrderPlacedEvent"/>.</summary>
public record OrderItemEvent(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

/// <summary>
/// Published by OrderService when an admin, store manager, or delivery driver
/// updates an order's status. Consumed by NotificationService to send the
/// appropriate in-app notification and transactional email to the customer.
/// </summary>
public record OrderStatusChangedEvent(
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    string NewStatus,
    decimal TotalAmount,
    DateTime ChangedAt);

/// <summary>
/// Published by PaymentService when a Razorpay webhook confirms payment capture.
/// Consumed by OrderService to automatically transition the order status to Processing.
/// </summary>
public record PaymentCompletedEvent(
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    decimal Amount);
