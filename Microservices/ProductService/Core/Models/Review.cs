namespace ProductService.Core.Models;

/// <summary>
/// Stores a customer's review for a product.
/// We only allow one review per customer per product.
/// </summary>
public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public Guid CustomerId { get; set; }

    /// <summary>
    /// We save the customer name here at the time of review so we don't have
    /// to call AuthService every time we want to display reviews.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Star rating, must be between 1 and 5.</summary>
    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
