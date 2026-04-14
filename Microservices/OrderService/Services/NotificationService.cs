using System.Net.Http.Json;

namespace OrderService.Services;

/// <summary>
/// Represents a single line item in an order confirmation or status email.
/// Used to build the items table in transactional email templates.
/// </summary>
/// <param name="ProductName">Display name of the product.</param>
/// <param name="Quantity">Number of units ordered.</param>
/// <param name="UnitPrice">Price per unit at the time of ordering.</param>
public record EmailOrderItem(string ProductName, int Quantity, decimal UnitPrice);

/// <summary>
/// HTTP client wrapper for communicating with the NotificationService.
/// Sends in-app (SignalR) notifications and transactional emails on behalf of OrderService.
/// All methods swallow exceptions and log warnings so notification failures
/// never block the primary order flow.
/// </summary>
public class NotificationService(HttpClient http, ILogger<NotificationService> logger)
{
    /// <summary>
    /// Sends a real-time in-app notification to a specific user via NotificationService.
    /// The notification is persisted in the database and pushed over SignalR.
    /// </summary>
    /// <param name="userId">Target user's ID.</param>
    /// <param name="title">Short notification title shown in the notification bell.</param>
    /// <param name="message">Full notification message body.</param>
    /// <param name="type">Visual type hint: <c>info</c>, <c>success</c>, <c>warning</c>, <c>error</c>, or <c>order</c>.</param>
    /// <param name="link">Optional deep-link URL the user is navigated to when clicking the notification.</param>
    public async Task SendToUserAsync(Guid userId, string title, string message, string type = "info", string? link = null)
    {
        try { await http.PostAsJsonAsync("api/v1/notifications/internal/user", new { userId, title, message, type, link }); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to send notification to user {UserId}", userId); }
    }

    /// <summary>
    /// Broadcasts a real-time in-app notification to all connected users with a specific role.
    /// Role broadcasts are not persisted — they are live-only SignalR pushes.
    /// </summary>
    /// <param name="role">Target role (e.g., <c>Admin</c>, <c>StoreManager</c>, <c>DeliveryDriver</c>).</param>
    /// <param name="title">Short notification title.</param>
    /// <param name="message">Full notification message body.</param>
    /// <param name="type">Visual type hint.</param>
    /// <param name="link">Optional deep-link URL.</param>
    public async Task SendToRoleAsync(string role, string title, string message, string type = "info", string? link = null)
    {
        try { await http.PostAsJsonAsync("api/v1/notifications/internal/role", new { role, title, message, type, link }); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to send notification to role {Role}", role); }
    }

    /// <summary>
    /// Sends an order confirmation email to the customer after successful payment.
    /// Includes the full order items table, total amount, delivery address, and estimated delivery date.
    /// </summary>
    /// <param name="email">Customer's email address.</param>
    /// <param name="firstName">Customer's first name for personalisation.</param>
    /// <param name="orderId">The FreshMart order ID.</param>
    /// <param name="totalAmount">Total amount charged in INR.</param>
    /// <param name="items">List of ordered items for the email items table.</param>
    /// <param name="deliveryAddress">Delivery address shown in the email.</param>
    /// <param name="estimatedDelivery">Human-readable estimated delivery date string.</param>
    public async Task SendOrderPlacedEmailAsync(string email, string firstName, string orderId, decimal totalAmount, List<EmailOrderItem> items, string deliveryAddress, string? estimatedDelivery)
    {
        try { await http.PostAsJsonAsync("api/v1/notifications/internal/email/order-placed", new { email, firstName, orderId, totalAmount, items, deliveryAddress, estimatedDelivery }); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to send order placed email"); }
    }

    /// <summary>
    /// Sends a transactional status update email to the customer when their order status changes.
    /// Triggered for statuses: <c>Processing</c>, <c>Shipped</c>, <c>OutForDelivery</c>, <c>Delivered</c>, <c>Cancelled</c>.
    /// </summary>
    /// <param name="email">Customer's email address.</param>
    /// <param name="firstName">Customer's first name for personalisation.</param>
    /// <param name="orderId">The FreshMart order ID.</param>
    /// <param name="status">The new order status string.</param>
    /// <param name="totalAmount">Order total shown in the email for reference.</param>
    public async Task SendOrderStatusEmailAsync(string email, string firstName, string orderId, string status, decimal totalAmount)
    {
        try { await http.PostAsJsonAsync("api/v1/notifications/internal/email/order-status", new { email, firstName, orderId, status, totalAmount }); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to send order status email"); }
    }
}
