using System.Net.Http.Json;

namespace OrderService.Services;

public record EmailOrderItem(string ProductName, int Quantity, decimal UnitPrice);

public class NotificationService(HttpClient http, ILogger<NotificationService> logger)
{
    public async Task SendToUserAsync(Guid userId, string title, string message, string type = "info", string? link = null)
    {
        try { await http.PostAsJsonAsync("api/v1/notifications/internal/user", new { userId, title, message, type, link }); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to send notification to user {UserId}", userId); }
    }

    public async Task SendToRoleAsync(string role, string title, string message, string type = "info", string? link = null)
    {
        try { await http.PostAsJsonAsync("api/v1/notifications/internal/role", new { role, title, message, type, link }); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to send notification to role {Role}", role); }
    }

    public async Task SendOrderPlacedEmailAsync(string email, string firstName, string orderId, decimal totalAmount, List<EmailOrderItem> items, string deliveryAddress, string? estimatedDelivery)
    {
        try { await http.PostAsJsonAsync("api/v1/notifications/internal/email/order-placed", new { email, firstName, orderId, totalAmount, items, deliveryAddress, estimatedDelivery }); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to send order placed email"); }
    }

    public async Task SendOrderStatusEmailAsync(string email, string firstName, string orderId, string status, decimal totalAmount)
    {
        try { await http.PostAsJsonAsync("api/v1/notifications/internal/email/order-status", new { email, firstName, orderId, status, totalAmount }); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to send order status email"); }
    }
}