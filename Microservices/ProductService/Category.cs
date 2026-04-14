namespace ProductService.Models;

/// <summary>
/// Represents a product category in the FreshMart catalogue.
/// Supports a single level of hierarchy via <see cref="ParentCategoryId"/>,
/// allowing sub-categories (e.g., "Fruits" under "Fruits &amp; Vegetables").
/// </summary>
public class Category
{
    /// <summary>Unique identifier for the category (primary key).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name of the category shown in navigation and filters (e.g., "Dairy &amp; Eggs").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description providing more context about what the category contains.</summary>
    public string? Description { get; set; }

    /// <summary>Optional URL for a category banner or icon image used in the UI.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Optional foreign key to a parent category, enabling a two-level hierarchy.
    /// Null for top-level categories.
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>Navigation property to the parent category. Null for root-level categories.</summary>
    public Category? ParentCategory { get; set; }

    /// <summary>Collection of all products belonging to this category.</summary>
    public ICollection<Product> Products { get; set; } = [];
}
