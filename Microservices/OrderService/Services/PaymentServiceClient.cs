using System.Net.Http.Json;

namespace OrderService.Services;

public class PaymentServiceClient(HttpClient http)
{
    public async Task<PaymentOrderResult?> CreatePaymentOrderAsync(Guid orderId, decimal amount, string bearerToken)
    {
        try
        {
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            var response = await http.PostAsJsonAsync("/api/v1/payment/create-order", new
            {
                orderId, amount, currency = "INR", notes = $"Order {orderId}"
            });
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<PaymentOrderResult>();
        }
        catch { return null; }
    }
}

public record PaymentOrderResult(string RazorpayOrderId, string RazorpayKeyId, decimal Amount, string Currency, string OrderId);
