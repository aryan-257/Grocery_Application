using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController(AppDbContext db, NotificationService notif, IPaymentService paymentService, EmailService email) : ControllerBase
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
        // Delivery drivers only see orders in transit
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
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart == null || !cart.Items.Any()) return BadRequest(new { error = "Cart is empty" });

        var subTotal = cart.Items.Sum(i => {
            var unitPrice = i.Product.DiscountPercent > 0
                ? Math.Round(i.Product.Price * (1 - i.Product.DiscountPercent / 100m), 2)
                : i.Product.Price;
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
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitPrice = i.Product.DiscountPercent > 0
                    ? Math.Round(i.Product.Price * (1 - i.Product.DiscountPercent / 100m), 2)
                    : i.Product.Price
            }).ToList()
        };
        
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Create Razorpay order
        try
        {
            var paymentOrderRequest = new CreatePaymentOrderRequest
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Currency = "INR",
                Notes = $"Order payment for {order.Id}"
            };
            
            var paymentOrder = await paymentService.CreatePaymentOrderAsync(paymentOrderRequest, UserId);
            
            // Return order with Razorpay order ID
            var orderDto = ToDto(order);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new
            {
                order = orderDto,
                razorpayOrderId = paymentOrder.RazorpayOrderId,
                razorpayKey = paymentOrder.RazorpayKeyId
            });
        }
        catch (Exception ex)
        {
            // If Razorpay order creation fails, still return the order but log the error
            // This ensures the order is created even if payment gateway is down
            Console.WriteLine($"Failed to create Razorpay order: {ex.Message}");
            
            // Clear cart anyway since order was created
            db.CartItems.RemoveRange(cart.Items);
            await db.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, ToDto(order));
        }
    }

    [HttpPost("{id}/complete-payment")]
    public async Task<IActionResult> CompletePayment(Guid id)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        if (order.CustomerId != UserId) return Forbid();

        // Clear cart after successful payment
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == UserId);
        if (cart != null)
        {
            db.CartItems.RemoveRange(cart.Items);
            await db.SaveChangesAsync();
        }

        // Send notifications
        await notif.SendToUserAsync(UserId,
            "Payment Successful",
            $"Payment for order #{order.Id.ToString()[..8].ToUpper()} completed successfully. Total: Rs.{order.TotalAmount:F2}",
            "success", $"/orders/{order.Id}/track");

        // Notify admins/managers
        await notif.SendToRoleAsync("Admin", "New Order Received",
            $"Order #{order.Id.ToString()[..8].ToUpper()} placed for Rs.{order.TotalAmount:F2}", "order", "/admin/orders");
        await notif.SendToRoleAsync("StoreManager", "New Order Received",
            $"Order #{order.Id.ToString()[..8].ToUpper()} placed for Rs.{order.TotalAmount:F2}", "order", "/admin/orders");

        // Send order confirmation email
        var user = await db.Users.FindAsync(UserId);
        if (user != null)
            await email.SendOrderPlacedAsync(user, order);

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

        // Notify the customer about their order status change
        var (title, msg, type) = req.Status switch
        {
            "Processing"     => ("Order Processing",      $"Your order #{id.ToString()[..8].ToUpper()} is being prepared.", "info"),
            "Shipped"        => ("Order Shipped",         $"Your order #{id.ToString()[..8].ToUpper()} has been shipped!", "info"),
            "OutForDelivery" => ("Out for Delivery",      $"Your order #{id.ToString()[..8].ToUpper()} is out for delivery!", "warning"),
            "Delivered"      => ("Order Delivered",       $"Your order #{id.ToString()[..8].ToUpper()} has been delivered. Enjoy!", "success"),
            "Cancelled"      => ("Order Cancelled",       $"Your order #{id.ToString()[..8].ToUpper()} has been cancelled.", "error"),
            _                => ("Order Updated",         $"Your order #{id.ToString()[..8].ToUpper()} status: {req.Status}", "info")
        };
        await notif.SendToUserAsync(order.CustomerId, title, msg, type, $"/orders/{id}/track");

        // Send status email to customer
        var customer = await db.Users.FindAsync(order.CustomerId);
        if (customer != null)
        {
            var emailTask = req.Status switch
            {
                "Processing"     => email.SendOrderProcessingAsync(customer, order),
                "Shipped"        => email.SendOrderShippedAsync(customer, order),
                "OutForDelivery" => email.SendOutForDeliveryAsync(customer, order),
                "Delivered"      => email.SendOrderDeliveredAsync(customer, order),
                "Cancelled"      => email.SendOrderCancelledAsync(customer, order),
                _                => Task.CompletedTask
            };
            await emailTask;
        }

        // Notify delivery drivers when order is ready to ship
        if (req.Status == "Shipped" || req.Status == "Processing")
            await notif.SendToRoleAsync("DeliveryDriver", "New Delivery Available",
                $"Order #{id.ToString()[..8].ToUpper()} is ready for pickup.", "order", "/delivery");

        return NoContent();
    }
}
