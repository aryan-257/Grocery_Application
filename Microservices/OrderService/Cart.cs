namespace OrderService.Models;

/// <summary>
/// Represents a customer's shopping cart.
/// Each customer has at most one active cart. Items are persisted so the cart
/// survives page refreshes and browser sessions.
/// </summary>
public class Cart
{
    /// <summary>Unique identifier for the cart (primary key).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ID of the customer who owns this cart. One cart per customer.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Optional spending limit set by the customer.
    /// When the cart total exceeds this value, the frontend shows a budget warning.
    /// </summary>
    public decimal? BudgetLimit { get; set; }

    /// <summary>UTC timestamp of the last modification to the cart (item add, update, or remove).</summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>The products currently in the cart with their quantities.</summary>
    public ICollection<CartItem> Items { get; set; } = [];
}

/// <summary>
/// Represents a single product entry in a customer's <see cref="Cart"/>.
/// Holds a reference to the locally cached product for price and image display.
/// </summary>
public class CartItem
{
    /// <summary>Unique identifier for this cart item.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Foreign key to the parent <see cref="Cart"/>.</summary>
    public Guid CartId { get; set; }

    /// <summary>Navigation property back to the parent cart.</summary>
    public Cart Cart { get; set; } = null!;

    /// <summary>ID of the product added to the cart. References the local product cache.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Navigation property to the locally cached product, used for price and image display.</summary>
    public Product Product { get; set; } = null!;

    /// <summary>Number of units of this product in the cart.</summary>
    public int Quantity { get; set; }
}
