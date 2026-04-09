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

using OrderService.Services;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController(OrderDbContext db, NotificationService notif, ProductServiceClient productClient, PaymentServiceClient paymentClient) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID claim not found"));

    private static OrderDto ToDto(Order o) => new(
        o.Id.ToString(), o.CustomerId.ToString(), o.Status,
        o.SubTotal, o.DeliveryFee, o.TaxAmount, o.DiscountAmount, o.TotalAmount,
        o.DeliveryAddress, o.Notes, o.CreatedAt.ToString("o"),
        o.EstimatedDelivery?.ToString("o"), o.DeliveredAt?.ToString("o"),
        o.Items.Select(i => new OrderItemDto(i.ProductId.ToString(), i.ProductName, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)));

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("StoreManager") || User.IsInRole("DeliveryDriver");
        var q = db.Orders.Include(o => o.Items).AsQueryable();
        if (!isAdmin) q = q.Where(o => o.CustomerId == UserId);
        if (User.IsInRole("DeliveryDriver"))
            q = q.Where(o => o.Status == "Shipped" || o.Status == "OutForDelivery" || o.Status == "Delivered");
        var orders = await q.OrderByDescending(o => o.CreatedAt).Select(o => ToDto(o)).ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        if (order.CustomerId != UserId && !User.IsInRole("Admin") && !User.IsInRole("StoreManager"))
            return Forbid();
        return Ok(ToDto(order));
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest req)
    {
        var cart = await db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null || !cart.Items.Any()) return BadRequest(new { error = "Cart is empty" });

        // Load product prices from local product cache
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await db.Set<Product>()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var subTotal = cart.Items.Sum(i => {
            if (!products.TryGetValue(i.ProductId, out var product)) return 0m;
            var unitPrice = product.DiscountPercent > 0
                ? Math.Round(product.Price * (1 - product.DiscountPercent / 100m), 2)
                : product.Price;
            return unitPrice * i.Quantity;
        });
        var deliveryFee = subTotal >= 500 ? 0m : 49m;
        var tax = Math.Round(subTotal * 0.05m, 2);

        decimal discount = 0;
        if (!string.IsNullOrWhiteSpace(req.CouponCode))
        {
            var coupon = await db.Coupons.FirstOrDefaultAsync(c =>
                c.Code == req.CouponCode.ToUpper() && c.IsActive &&
                (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow) &&
                c.UsedCount < c.UsageLimit && subTotal >= c.MinOrderAmount);
            if (coupon != null)
            {
                discount = coupon.DiscountType == "Percentage"
                    ? Math.Round(subTotal * coupon.DiscountValue / 100, 2)
                    : Math.Min(coupon.DiscountValue, subTotal);
                coupon.UsedCount++;
            }
        }

        var order = new Order
        {
            CustomerId = UserId,
            DeliveryAddress = req.DeliveryAddress,
            Notes = req.Notes,
            SubTotal = subTotal,
            DeliveryFee = deliveryFee,
            TaxAmount = tax,
            DiscountAmount = discount,
            TotalAmount = Math.Max(0, subTotal + deliveryFee + tax - discount),
            EstimatedDelivery = DateTime.UtcNow.AddDays(2),
            Items = cart.Items.Select(i => {
                products.TryGetValue(i.ProductId, out var product);
                return new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = product?.Name ?? "Unknown",
                    Quantity = i.Quantity,
                    UnitPrice = product != null && product.DiscountPercent > 0
                        ? Math.Round(product.Price * (1 - product.DiscountPercent / 100m), 2)
                        : product?.Price ?? 0m
                };
            }).ToList()
        };

        db.Orders.Add(order);
        db.CartItems.RemoveRange(cart.Items);
        await db.SaveChangesAsync();

        // Call payment service to create Razorpay order
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var paymentResult = await paymentClient.CreatePaymentOrderAsync(order.Id, order.TotalAmount, token);

        if (paymentResult != null)
        {
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new
            {
                order = ToDto(order),
                razorpayOrderId = paymentResult.RazorpayOrderId,
                razorpayKey = paymentResult.RazorpayKeyId
            });
        }

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, ToDto(order));
    }

    [HttpPost("{id}/complete-payment")]
    public async Task<IActionResult> CompletePayment(Guid id)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        if (order.CustomerId != UserId) return Forbid();

        // Clear cart
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart != null) { db.CartItems.RemoveRange(cart.Items); await db.SaveChangesAsync(); }

        // Send in-app notifications
        await notif.SendToUserAsync(UserId,
            "Payment Successful",
            $"Payment for order #{order.Id.ToString()[..8].ToUpper()} completed. Total: Rs.{order.TotalAmount:F2}",
            "success", $"/orders/{order.Id}/track");
        await notif.SendToRoleAsync("Admin", "New Order Received",
            $"Order #{order.Id.ToString()[..8].ToUpper()} placed for Rs.{order.TotalAmount:F2}", "order", "/admin/orders");
        await notif.SendToRoleAsync("StoreManager", "New Order Received",
            $"Order #{order.Id.ToString()[..8].ToUpper()} placed for Rs.{order.TotalAmount:F2}", "order", "/admin/orders");

        // Send confirmation email via notification service
        // Get user info from JWT claims (avoids cross-service DB dependency)
        var userEmail = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)
            ?? User.FindFirstValue(ClaimTypes.Email) ?? "";
        var userFirstName = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.GivenName)
            ?? User.FindFirstValue(ClaimTypes.GivenName) ?? "Customer";

        if (!string.IsNullOrEmpty(userEmail))
        {
            var items = order.Items.Select(i => new EmailOrderItem(i.ProductName, i.Quantity, i.UnitPrice)).ToList();
            await notif.SendOrderPlacedEmailAsync(userEmail, userFirstName, order.Id.ToString(), order.TotalAmount, items, order.DeliveryAddress, order.EstimatedDelivery?.ToString("dddd, MMMM dd yyyy"));
        }

        return Ok(new { message = "Payment completed successfully" });
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,StoreManager,DeliveryDriver")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateOrderStatusRequest req)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        order.Status = req.Status;
        if (req.Status == "Delivered") order.DeliveredAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var (title, msg, type) = req.Status switch
        {
            "Processing"     => ("Order Processing",  $"Your order #{id.ToString()[..8].ToUpper()} is being prepared.", "info"),
            "Shipped"        => ("Order Shipped",     $"Your order #{id.ToString()[..8].ToUpper()} has been shipped!", "info"),
            "OutForDelivery" => ("Out for Delivery",  $"Your order #{id.ToString()[..8].ToUpper()} is out for delivery!", "warning"),
            "Delivered"      => ("Order Delivered",   $"Your order #{id.ToString()[..8].ToUpper()} has been delivered. Enjoy!", "success"),
            "Cancelled"      => ("Order Cancelled",   $"Your order #{id.ToString()[..8].ToUpper()} has been cancelled.", "error"),
            _                => ("Order Updated",     $"Your order #{id.ToString()[..8].ToUpper()} status: {req.Status}", "info")
        };
        await notif.SendToUserAsync(order.CustomerId, title, msg, type, $"/orders/{id}/track");

        // Send status email - fetch customer from AuthService
        try {
            var authToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            var authUrl = System.Environment.GetEnvironmentVariable("Services__AuthService") ?? "http://auth-service:5001";
            var customerResp = await httpClient.GetAsync($"{authUrl}/api/v1/users/{order.CustomerId}");
            if (customerResp.IsSuccessStatusCode) {
                var customerData = await customerResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                var custEmail = customerData.GetProperty("email").GetString() ?? "";
                var custName = customerData.GetProperty("firstName").GetString() ?? "Customer";
                if (!string.IsNullOrEmpty(custEmail) && new[] { "Processing","Shipped","OutForDelivery","Delivered","Cancelled" }.Contains(req.Status))
                    await notif.SendOrderStatusEmailAsync(custEmail, custName, id.ToString(), req.Status, order.TotalAmount);
            }
        } catch { /* email is non-critical */ }

        if (req.Status == "Shipped" || req.Status == "Processing")
            await notif.SendToRoleAsync("DeliveryDriver", "New Delivery Available",
                $"Order #{id.ToString()[..8].ToUpper()} is ready for pickup.", "order", "/delivery");

        return NoContent();
    }
}
