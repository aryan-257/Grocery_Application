namespace ProductService.Core.DTOs;

/// <summary>
/// Product details returned to the client.
/// DiscountedPrice is the final price after applying the discount.
/// If no discount, DiscountedPrice equals Price.
/// </summary>
public record ProductDto(
    string Id,
    string Name,
    string Description,
    decimal Price,
    string Sku,
    string ImageUrl,
    string CategoryName,
    int StockQuantity,
    bool IsActive,
    double AverageRating,
    string? Brand,
    string? Unit,
    decimal DiscountPercent,
    decimal DiscountedPrice);

public record CategoryDto(
    string Id,
    string Name,
    string? Description,
    string? ImageUrl,
    string? ParentCategoryId);

public record ReviewDto(
    string Id,
    string ProductId,
    string CustomerId,
    string CustomerName,
    int Rating,
    string Comment,
    string CreatedAt);

/// <summary>Used for paginated list responses. Total is the full count, not just this page.</summary>
public record PaginatedResult<T>(
    IEnumerable<T> Items,
    int Total,
    int Page,
    int PageSize);

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    string Sku,
    string ImageUrl,
    Guid CategoryId,
    int StockQuantity,
    string? Brand,
    string? Unit);

/// <summary>Used for PUT — all fields required, everything gets replaced.</summary>
public record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    string Sku,
    string ImageUrl,
    Guid CategoryId,
    int StockQuantity,
    string? Brand,
    string? Unit,
    decimal DiscountPercent,
    bool IsActive);

public record UpdateStockRequest(int Quantity);

public record UpdateDiscountRequest(decimal DiscountPercent);

public record CreateReviewRequest(int Rating, string Comment);
