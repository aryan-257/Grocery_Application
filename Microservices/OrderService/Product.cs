namespace OrderService.Models;

/// <summary>
/// Local product projection for cart/order calculations — not a cross-service dependency.
/// Populated via product service API calls or cached data.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public string ImageUrl { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}
