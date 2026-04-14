using System.Net.Http.Json;

namespace OrderService.Services;

/// <summary>
/// HTTP client wrapper for communicating with the PaymentService.
/// Used by OrderService to initiate Razorpay payment orders immediately after
/// an order is created, so the frontend can launch the Razorpay checkout modal.
/// Failures are swallowed and return null so the order creation still succeeds
/// even if PaymentService is temporarily unavailable.
/// </summary>
public class PaymentServiceClient(HttpClient http)
{
    /// <summary>
    /// Calls PaymentService to create a Razorpay order for the given FreshMart order.
    /// Attaches the caller's JWT so PaymentService can authenticate the request.
    /// Returns null if the call fails (network error, non-2xx response, or deserialization failure).
    /// </summary>
    /// <param name="orderId">The FreshMart order ID to associate with the payment.</param>
    /// <param name="amount">The total amount to charge in INR.</param>
    /// <param name="bearerToken">The customer's JWT access token, forwarded to PaymentService.</param>
    /// <returns>
    /// A <see cref="PaymentOrderResult"/> containing the Razorpay order ID and key,
    /// or null if the request failed.
    /// </returns>
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

/// <summary>
/// Represents the response from PaymentService after a Razorpay order is created.
/// Contains the data needed by the frontend to initialise the Razorpay checkout modal.
/// </summary>
/// <param name="RazorpayOrderId">The Razorpay-generated order ID (e.g., <c>order_XXXX</c>).</param>
/// <param name="RazorpayKeyId">The Razorpay publishable key ID used to initialise the frontend SDK.</param>
/// <param name="Amount">The order amount in INR.</param>
/// <param name="Currency">The currency code (always <c>INR</c>).</param>
/// <param name="OrderId">The FreshMart order ID as a string.</param>
public record PaymentOrderResult(string RazorpayOrderId, string RazorpayKeyId, decimal Amount, string Currency, string OrderId);
