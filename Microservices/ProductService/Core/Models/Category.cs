namespace ProductService.Core.Models;

/// <summary>
/// Product category. A category can have a parent category,
/// for example "Fruits" can be under "Fruits and Vegetables".
/// </summary>
public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>Null if this is a top-level category.</summary>
    public Guid? ParentCategoryId { get; set; }

    public Category? ParentCategory { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
