namespace OrderService.Core.Models;

/// <summary>
/// Shopping cart for a customer. Each customer has one cart.
/// Cart persists in the database so items are not lost on page refresh.
/// </summary>
public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }

    /// <summary>Optional budget limit. If set, the API will warn when cart total exceeds it.</summary>
    public decimal? BudgetLimit { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> Items { get; set; } = [];
}

/// <summary>A single product entry inside the cart with quantity.</summary>
public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CartId { get; set; }

    public Cart Cart { get; set; } = null!;

    public Guid ProductId { get; set; }

    /// <summary>We keep a local copy of the product so we can show price and image without calling ProductService.</summary>
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
}
