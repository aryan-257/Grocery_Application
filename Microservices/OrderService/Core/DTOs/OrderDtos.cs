namespace OrderService.Core.DTOs;

// Order DTOs

public record OrderItemDto(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

/// <summary>Full order details sent back to the client after placing or fetching an order.</summary>
public record OrderDto(
    string Id,
    string CustomerId,
    string Status,
    decimal SubTotal,
    decimal DeliveryFee,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string DeliveryAddress,
    string? Notes,
    string CreatedAt,
    string? EstimatedDelivery,
    string? DeliveredAt,
    IEnumerable<OrderItemDto> Items);

/// <summary>Request body when placing a new order from the cart.</summary>
public record CreateOrderRequest(
    string DeliveryAddress,
    string? Notes,
    string? CouponCode);

public record UpdateOrderStatusRequest(string Status);

// Cart DTOs

/// <summary>
/// UnitPrice is the price after discount.
/// OriginalPrice is the price before discount.
/// </summary>
public record CartItemDto(
    string ProductId,
    string ProductName,
    decimal UnitPrice,
    string ImageUrl,
    int Quantity,
    decimal TotalPrice,
    decimal DiscountPercent,
    decimal OriginalPrice);

/// <summary>
/// Full cart state returned to the client.
/// IsOverBudget is true if the total is more than the customer's set budget limit.
/// </summary>
public record CartDto(
    string CustomerId,
    IEnumerable<CartItemDto> Items,
    decimal? BudgetLimit,
    string LastUpdated,
    decimal SubTotal,
    bool IsOverBudget,
    int TotalItems);

public record AddToCartRequest(Guid ProductId, int Quantity);

public record UpdateCartItemRequest(int Quantity);

public record SetBudgetRequest(decimal? BudgetLimit);

// Coupon DTOs

public record CouponDto(
    string Code,
    string DiscountType,
    decimal DiscountValue,
    decimal MinOrderAmount,
    DateTime? ExpiresAt);

public record CouponValidateRequest(string Code, decimal OrderAmount);

public record CouponValidateResponse(
    bool IsValid,
    string Message,
    string? DiscountType,
    decimal DiscountValue,
    decimal DiscountAmount);
