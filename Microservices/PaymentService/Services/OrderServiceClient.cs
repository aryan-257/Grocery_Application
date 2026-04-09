using System.Net.Http.Json;

namespace PaymentService.Services;

public class OrderServiceClient(HttpClient http)
{
    public async Task<OrderInfo?> GetOrderAsync(Guid orderId, string bearerToken)
    {
        try
        {
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            return await http.GetFromJsonAsync<OrderInfo>($"/api/v1/orders/{orderId}");
        }
        catch { return null; }
    }
}

public record OrderInfo(string Id, string CustomerId, string Status, decimal TotalAmount);
