namespace ProductService.Models;

/// <summary>
/// Represents a customer review for a product.
/// Reviews are gated behind purchase verification — only customers who have
/// ordered the product (and not yet reviewed it) may submit a review.
/// </summary>
public class Review
{
    /// <summary>Unique identifier for the review (primary key).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Foreign key to the product being reviewed.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Navigation property to the reviewed product.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>ID of the customer who submitted the review. Used to enforce one-review-per-customer.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Display name of the reviewer, snapshotted at review creation time.
    /// Stored directly to avoid a cross-service lookup when rendering reviews.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Star rating given by the customer. Must be between 1 (worst) and 5 (best).</summary>
    public int Rating { get; set; } // 1-5

    /// <summary>Written feedback from the customer about the product.</summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>UTC timestamp of when the review was submitted.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
