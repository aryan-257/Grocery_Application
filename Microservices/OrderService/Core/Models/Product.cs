namespace OrderService.Core.Models;

/// <summary>
/// Local copy of product data stored in OrderService DB.
/// We save this when a user adds an item to cart so we don't have to
/// call ProductService again when creating the order.
/// </summary>
public class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    /// <summary>0 means no active discount.</summary>
    public decimal DiscountPercent { get; set; } = 0;

    public string ImageUrl { get; set; } = string.Empty;

    public int StockQuantity { get; set; }
}
