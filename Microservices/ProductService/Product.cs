namespace ProductService.Models;

/// <summary>
/// Represents a grocery product listed in the FreshMart catalogue.
/// Holds pricing, inventory, categorization, and display metadata.
/// </summary>
public class Product
{
    /// <summary>Unique identifier for the product (primary key).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name of the product shown in listings and search results.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Detailed description of the product, including size, origin, or key features.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Base price of the product in INR before any discount is applied.</summary>
    public decimal Price { get; set; }

    /// <summary>Stock Keeping Unit — a unique alphanumeric code used for inventory tracking.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>URL of the product's primary display image.</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Foreign key linking this product to its parent <see cref="Category"/>.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Navigation property to the product's category. Loaded via <c>Include</c> in queries.</summary>
    public Category Category { get; set; } = null!;

    /// <summary>Current number of units available in stock. Used to enforce stock checks at checkout.</summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Indicates whether the product is visible in the catalogue.
    /// Soft-delete flag — set to <c>false</c> instead of physically removing the record.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Computed average of all customer review ratings (1–5).
    /// Recalculated each time a new review is submitted.
    /// </summary>
    public double AverageRating { get; set; }

    /// <summary>Brand or manufacturer name (e.g., "Amul", "Tata"). Optional.</summary>
    public string? Brand { get; set; }

    /// <summary>Unit of measure for the product (e.g., "1kg", "500ml", "dozen"). Optional.</summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Discount percentage applied to the base price (0–100).
    /// A value of 0 means no discount. The discounted price is computed as:
    /// <c>Price * (1 - DiscountPercent / 100)</c>.
    /// </summary>
    public decimal DiscountPercent { get; set; } = 0; // 0-100

    /// <summary>UTC timestamp of when the product was first added to the catalogue.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
