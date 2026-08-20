using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Core.DTOs;
using OrderService.Core.Models;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController(OrderDbContext db, ProductServiceClient productClient) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID not found in token"));

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var cart = await db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == UserId);

        if (cart == null)
            return Ok(new CartDto(UserId.ToString(), [], null, DateTime.UtcNow.ToString("o"), 0, false, 0));

        return Ok(BuildCartDto(cart));
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddToCartRequest req)
    {
        var product = await productClient.GetProductAsync(req.ProductId);
        if (product == null)
            return NotFound(new { error = "Product not found" });

        if (product.StockQuantity < req.Quantity)
            return BadRequest(new { error = "Not enough stock" });

        // save product info locally so we dont need to call ProductService again at checkout
        var local = await db.Products.FindAsync(req.ProductId);
        if (local == null)
        {
            db.Products.Add(new Product
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                DiscountPercent = product.DiscountPercent,
                StockQuantity = product.StockQuantity
            });
        }
        else
        {
            local.Price = product.Price;
            local.DiscountPercent = product.DiscountPercent;
            local.StockQuantity = product.StockQuantity;
        }

        var cart = await db.Carts.FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null)
        {
            cart = new Cart { CustomerId = UserId };
            db.Carts.Add(cart);
            await db.SaveChangesAsync();
        }

        var existing = await db.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == req.ProductId);

        if (existing != null)
            existing.Quantity += req.Quantity;
        else
            db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = req.ProductId, Quantity = req.Quantity });

        cart.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var full = await db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstAsync(c => c.Id == cart.Id);

        return Ok(BuildCartDto(full));
    }

    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveItem(Guid productId)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null) return NotFound();

        var item = await db.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == productId);

        if (item != null)
        {
            db.CartItems.Remove(item);
            cart.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var full = await db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstAsync(c => c.Id == cart.Id);

        return Ok(BuildCartDto(full));
    }

    private static CartDto BuildCartDto(Cart cart)
    {
        var items = cart.Items.Select(i =>
        {
            var unitPrice = i.Product.DiscountPercent > 0
                ? Math.Round(i.Product.Price * (1 - i.Product.DiscountPercent / 100m), 2)
                : i.Product.Price;

            return new CartItemDto(
                i.ProductId.ToString(), i.Product.Name,
                unitPrice, i.Product.ImageUrl,
                i.Quantity, unitPrice * i.Quantity,
                i.Product.DiscountPercent, i.Product.Price);
        }).ToList();

        var subTotal = items.Sum(i => i.TotalPrice);

        return new CartDto(
            cart.CustomerId.ToString(), items,
            cart.BudgetLimit, cart.LastUpdated.ToString("o"),
            subTotal,
            cart.BudgetLimit.HasValue && subTotal > cart.BudgetLimit.Value,
            items.Sum(i => i.Quantity));
    }
}
