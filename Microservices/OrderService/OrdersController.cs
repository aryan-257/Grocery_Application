using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MassTransit;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Services;
using SharedModels.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Controllers;

/// <summary>
/// Manages the full order lifecycle for the FreshMart platform.
/// Publishes domain events to RabbitMQ via MassTransit for async processing
/// by NotificationService and ProductService.
/// Retains synchronous HTTP call to PaymentService for Razorpay order creation.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController(
    OrderDbContext db,
    PaymentServiceClient paymentClient,
    IPublishEndpoint? publishEndpoint,
    ILogger<OrdersController> logger) : ControllerBase
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

    /// <summary>
    /// Returns orders visible to the authenticated user.
    /// Customers see only their own orders. Admin/StoreManager see all.
    /// DeliveryDriver sees only Shipped/OutForDelivery/Delivered orders.
    /// </summary>
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

    /// <summary>Returns a single order by ID with ownership enforcement for customers.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        if (order.CustomerId != UserId && !User.IsInRole("Admin") && !User.IsInRole("StoreManager"))
            return Forbid();
        return Ok(ToDto(order));
    }

    /// <summary>
    /// Creates a new order from the customer's cart.
    /// Calculates totals, validates coupon, snapshots prices and customer info.
    /// Calls PaymentService synchronously to get Razorpay order ID for frontend checkout.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest req)
    {
        var cart = await db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null || !cart.Items.Any()) return BadRequest(new { error = "Cart is empty" });

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

        var customerEmail = User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? User.FindFirstValue(ClaimTypes.Email) ?? "";
        var customerFirstName = User.FindFirstValue(JwtRegisteredClaimNames.GivenName)
            ?? User.FindFirstValue(ClaimTypes.GivenName) ?? "Customer";
        var customerLastName = User.FindFirstValue(JwtRegisteredClaimNames.FamilyName)
            ?? User.FindFirstValue(ClaimTypes.Surname) ?? "";

        // Upsert local AppUser projection
        var appUser = await db.Users.FindAsync(UserId);
        if (appUser == null)
            db.Users.Add(new AppUser { Id = UserId, Email = customerEmail, FirstName = customerFirstName });
        else { appUser.Email = customerEmail; appUser.FirstName = customerFirstName; }

        var order = new Order
        {
            CustomerId = UserId,
            CustomerEmail = customerEmail,
            CustomerFirstName = customerFirstName,
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

        // Synchronous HTTP call to PaymentService — Razorpay order ID needed immediately
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var paymentResult = await paymentClient.CreatePaymentOrderAsync(order.Id, order.TotalAmount, token);

        if (paymentResult != null)
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new
            {
                order = ToDto(order),
                razorpayOrderId = paymentResult.RazorpayOrderId,
                razorpayKey = paymentResult.RazorpayKeyId
            });

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, ToDto(order));
    }

    /// <summary>
    /// Confirms payment completion: clears cart and publishes OrderPlacedEvent to RabbitMQ.
    /// NotificationService consumes the event to send in-app notifications and confirmation email.
    /// ProductService consumes the event to decrement stock.
    /// </summary>
    [HttpPost("{id}/complete-payment")]
    public async Task<IActionResult> CompletePayment(Guid id)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        if (order.CustomerId != UserId) return Forbid();

        // Clear cart
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart != null) { db.CartItems.RemoveRange(cart.Items); await db.SaveChangesAsync(); }

        // Backfill customer info snapshot if missing
        var userEmail = User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? User.FindFirstValue(ClaimTypes.Email) ?? order.CustomerEmail;
        var userFirstName = User.FindFirstValue(JwtRegisteredClaimNames.GivenName)
            ?? User.FindFirstValue(ClaimTypes.GivenName) ?? order.CustomerFirstName;
        var userLastName = User.FindFirstValue(JwtRegisteredClaimNames.FamilyName)
            ?? User.FindFirstValue(ClaimTypes.Surname) ?? "";

        if (string.IsNullOrEmpty(order.CustomerEmail) && !string.IsNullOrEmpty(userEmail))
        {
            order.CustomerEmail = userEmail;
            order.CustomerFirstName = userFirstName;
            await db.SaveChangesAsync();
        }

        // Publish OrderPlacedEvent to RabbitMQ — replaces direct HTTP calls to NotificationService and ProductService
        if (publishEndpoint != null)
        {
            try
            {
                await publishEndpoint.Publish(new OrderPlacedEvent(
                    OrderId: order.Id,
                    CustomerId: order.CustomerId,
                    CustomerEmail: userEmail,
                    CustomerName: $"{userFirstName} {userLastName}".Trim(),
                    TotalAmount: order.TotalAmount,
                    DeliveryAddress: order.DeliveryAddress,
                    Items: order.Items.Select(i => new OrderItemEvent(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
                    CreatedAt: order.CreatedAt));

                logger.LogInformation("Published OrderPlacedEvent for Order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish OrderPlacedEvent for Order {OrderId}", order.Id);
            }
        }

        return Ok(new { message = "Payment completed successfully" });
    }

    /// <summary>
    /// Updates order status and publishes OrderStatusChangedEvent to RabbitMQ.
    /// NotificationService consumes the event to send in-app notification and status email.
    /// Works for all roles: Admin, StoreManager, DeliveryDriver.
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,StoreManager,DeliveryDriver")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateOrderStatusRequest req)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        order.Status = req.Status;
        if (req.Status == "Delivered") order.DeliveredAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Resolve customer email — snapshot first, fallback to local AppUser projection
        var emailTo = order.CustomerEmail;
        var nameTo = order.CustomerFirstName;
        if (string.IsNullOrEmpty(emailTo))
        {
            var appUser = await db.Users.FindAsync(order.CustomerId);
            emailTo = appUser?.Email ?? "";
            nameTo = appUser?.FirstName ?? "Customer";
            if (!string.IsNullOrEmpty(emailTo))
            {
                order.CustomerEmail = emailTo;
                order.CustomerFirstName = nameTo;
                await db.SaveChangesAsync();
            }
        }

        // Publish OrderStatusChangedEvent to RabbitMQ — replaces direct HTTP calls to NotificationService
        if (publishEndpoint != null)
        {
            try
            {
                await publishEndpoint.Publish(new OrderStatusChangedEvent(
                    OrderId: order.Id,
                    CustomerId: order.CustomerId,
                    CustomerEmail: emailTo,
                    CustomerName: nameTo,
                    NewStatus: req.Status,
                    TotalAmount: order.TotalAmount,
                    ChangedAt: DateTime.UtcNow));

                logger.LogInformation("Published OrderStatusChangedEvent for Order {OrderId} → {Status}", id, req.Status);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish OrderStatusChangedEvent for Order {OrderId}", id);
            }
        }

        return NoContent();
    }
}
