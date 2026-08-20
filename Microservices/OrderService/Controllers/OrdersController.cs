using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Core.DTOs;
using OrderService.Core.Models;
using OrderService.Infrastructure.Data;

namespace OrderService.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController(OrderDbContext db) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID not found in token"));

    private bool IsAdmin => User.IsInRole("Admin") || User.IsInRole("StoreManager");

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var query = db.Orders.Include(o => o.Items).AsQueryable();

        // customers only see their own orders
        if (!IsAdmin)
            query = query.Where(o => o.CustomerId == UserId);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(MapToDto));
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest req)
    {
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email)
                 ?? User.FindFirstValue(ClaimTypes.Email) ?? "";
        var firstName = User.FindFirstValue(JwtRegisteredClaimNames.GivenName)
                     ?? User.FindFirstValue(ClaimTypes.GivenName) ?? "Customer";

        var cart = await db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == UserId);

        if (cart == null || !cart.Items.Any())
            return BadRequest(new { error = "Cart is empty" });

        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        decimal subTotal = cart.Items.Sum(i =>
        {
            if (!products.TryGetValue(i.ProductId, out var p)) return 0m;
            var unit = p.DiscountPercent > 0
                ? Math.Round(p.Price * (1 - p.DiscountPercent / 100m), 2)
                : p.Price;
            return unit * i.Quantity;
        });

        // free delivery above 500, otherwise 49
        var deliveryFee = subTotal >= 500m ? 0m : 49m;
        var tax = Math.Round(subTotal * 0.05m, 2);

        var appUser = await db.Users.FindAsync(UserId);
        if (appUser == null)
            db.Users.Add(new AppUser { Id = UserId, Email = email, FirstName = firstName });
        else
        {
            appUser.Email = email;
            appUser.FirstName = firstName;
        }

        var order = new Order
        {
            CustomerId = UserId,
            CustomerEmail = email,
            CustomerFirstName = firstName,
            DeliveryAddress = req.DeliveryAddress,
            Notes = req.Notes,
            SubTotal = subTotal,
            DeliveryFee = deliveryFee,
            TaxAmount = tax,
            DiscountAmount = 0,
            TotalAmount = subTotal + deliveryFee + tax,
            EstimatedDelivery = DateTime.UtcNow.AddDays(2),
            Items = cart.Items.Select(i =>
            {
                products.TryGetValue(i.ProductId, out var p);
                var unit = p != null && p.DiscountPercent > 0
                    ? Math.Round(p.Price * (1 - p.DiscountPercent / 100m), 2)
                    : p?.Price ?? 0m;
                return new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = p?.Name ?? "Unknown",
                    Quantity = i.Quantity,
                    UnitPrice = unit
                };
            }).ToList()
        };

        db.Orders.Add(order);
        db.CartItems.RemoveRange(cart.Items);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrders), new { }, MapToDto(order));
    }

    private static OrderDto MapToDto(Order o) => new(
        o.Id.ToString(),
        o.CustomerId.ToString(),
        o.Status,
        o.SubTotal,
        o.DeliveryFee,
        o.TaxAmount,
        o.DiscountAmount,
        o.TotalAmount,
        o.DeliveryAddress,
        o.Notes,
        o.CreatedAt.ToString("o"),
        o.EstimatedDelivery?.ToString("o"),
        o.DeliveredAt?.ToString("o"),
        o.Items.Select(i => new OrderItemDto(
            i.ProductId.ToString(), i.ProductName,
            i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)));
}
