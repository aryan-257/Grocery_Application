using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Controllers;

/// <summary>
/// Manages the authenticated customer's shopping cart.
/// Handles adding, updating, and removing items, clearing the cart,
/// and setting an optional budget limit.
/// Product data is fetched from ProductService on add and cached locally
/// to avoid repeated cross-service calls for price and image display.
/// All endpoints require authentication.
/// </summary>
[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController(OrderDbContext db, ProductServiceClient productClient) : ControllerBase
{
    /// <summary>
    /// Attempts to parse the authenticated user's ID from the JWT <c>sub</c> claim.
    /// Returns null if the claim is missing or not a valid GUID.
    /// </summary>
    private Guid? TryGetUserId()
    {
        var val = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(val, out var id) ? id : null;
    }

    /// <summary>
    /// Returns a 400 error with diagnostic claim information if the user ID cannot be resolved.
    /// Used as a guard before any cart mutation to provide a clear error instead of a silent failure.
    /// </summary>
    private IActionResult? UserIdError()
    {
        var id = TryGetUserId();
        if (id == null)
        {
            var claims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
            return Problem($"User ID claim not found. Claims: [{string.Join(", ", claims)}]", statusCode: 400);
        }
        return null;
    }

    /// <summary>Resolves the authenticated user's ID. Throws if the claim is missing.</summary>
    private Guid UserId => TryGetUserId()!.Value;

    /// <summary>
    /// Builds a <see cref="CartDto"/> from a fully loaded <see cref="Cart"/> entity.
    /// Computes discounted unit prices, line totals, subtotal, item count,
    /// and whether the budget limit has been exceeded.
    /// </summary>
    private async Task<CartDto> BuildCartDto(Cart cart)
    {
        var items = cart.Items.Select(i => {
            var unitPrice = i.Product.DiscountPercent > 0
                ? Math.Round(i.Product.Price * (1 - i.Product.DiscountPercent / 100m), 2)
                : i.Product.Price;
            return new CartItemDto(
                i.ProductId.ToString(), i.Product.Name, unitPrice,
                i.Product.ImageUrl, i.Quantity, unitPrice * i.Quantity,
                i.Product.DiscountPercent, i.Product.Price);
        }).ToList();
        var subTotal = items.Sum(i => i.TotalPrice);
        return new CartDto(cart.CustomerId.ToString(), items, cart.BudgetLimit,
            cart.LastUpdated.ToString("o"), subTotal,
            cart.BudgetLimit.HasValue && subTotal > cart.BudgetLimit.Value,
            items.Sum(i => i.Quantity));
    }

    /// <summary>
    /// Returns the authenticated customer's current cart with all items, prices, and totals.
    /// Returns an empty cart DTO if the customer has no cart yet.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null) return Ok(new CartDto(UserId.ToString(), [], null, DateTime.UtcNow.ToString("o"), 0, false, 0));
        return Ok(await BuildCartDto(cart));
    }

    /// <summary>
    /// Adds a product to the cart or increments its quantity if already present.
    /// Fetches the latest product data from ProductService and caches it locally.
    /// Validates stock availability before adding.
    /// Creates the cart if it does not yet exist for this customer.
    /// Returns the updated full cart DTO.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddToCartRequest req)
    {
        var err = UserIdError(); if (err != null) return err;
        var product = await productClient.GetProductAsync(req.ProductId);
        if (product == null) return NotFound(new { error = "Product not found" });
        if (product.StockQuantity < req.Quantity) return BadRequest(new { error = "Insufficient stock" });

        // Cache product info locally for cart display
        var localProduct = await db.Products.FindAsync(req.ProductId);
        if (localProduct == null)
        {
            localProduct = new Product
            {
                Id = product.Id, Name = product.Name, Price = product.Price,
                ImageUrl = product.ImageUrl, DiscountPercent = product.DiscountPercent,
                StockQuantity = product.StockQuantity
            };
            db.Products.Add(localProduct);
        }
        else
        {
            localProduct.Price = product.Price;
            localProduct.DiscountPercent = product.DiscountPercent;
            localProduct.StockQuantity = product.StockQuantity;
        }

        // Ensure cart exists first (save before adding items to get a valid CartId)
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null)
        {
            cart = new Cart { CustomerId = UserId };
            db.Carts.Add(cart);
            await db.SaveChangesAsync(); // persist so cart.Id is valid
        }

        // Now handle item — query directly to avoid stale tracking
        var existing = await db.CartItems.FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == req.ProductId);
        if (existing != null)
        {
            existing.Quantity += req.Quantity;
        }
        else
        {
            db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = req.ProductId, Quantity = req.Quantity });
        }

        cart.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var full = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstAsync(c => c.Id == cart.Id);
        return Ok(await BuildCartDto(full));
    }

    /// <summary>
    /// Updates the quantity of a specific item in the cart.
    /// If the quantity is set to 0 or less, the item is removed from the cart.
    /// Returns the updated full cart DTO.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpPut("items/{productId}")]
    public async Task<IActionResult> UpdateItem(Guid productId, UpdateCartItemRequest req)
    {
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null) return NotFound();
        var item = await db.CartItems.FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == productId);
        if (item == null) return NotFound();
        if (req.Quantity <= 0) db.CartItems.Remove(item);
        else item.Quantity = req.Quantity;
        cart.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var full = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstAsync(c => c.Id == cart.Id);
        return Ok(await BuildCartDto(full));
    }

    /// <summary>
    /// Removes a specific product from the cart entirely.
    /// Returns the updated full cart DTO after removal.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveItem(Guid productId)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null) return NotFound();
        var item = await db.CartItems.FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == productId);
        if (item != null) { db.CartItems.Remove(item); cart.LastUpdated = DateTime.UtcNow; await db.SaveChangesAsync(); }
        var full = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).FirstAsync(c => c.Id == cart.Id);
        return Ok(await BuildCartDto(full));
    }

    /// <summary>
    /// Removes all items from the customer's cart.
    /// Returns 204 No Content. The cart record itself is preserved for future use.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart != null) { db.CartItems.RemoveRange(cart.Items); cart.LastUpdated = DateTime.UtcNow; await db.SaveChangesAsync(); }
        return NoContent();
    }

    /// <summary>
    /// Sets or updates the optional budget limit for the customer's cart.
    /// When the cart total exceeds this limit, the frontend shows a budget warning.
    /// Pass null to remove the budget limit.
    /// Creates the cart if it does not yet exist.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpPut("budget")]
    public async Task<IActionResult> SetBudget(SetBudgetRequest req)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null) { cart = new Cart { CustomerId = UserId }; db.Carts.Add(cart); }
        cart.BudgetLimit = req.BudgetLimit;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
