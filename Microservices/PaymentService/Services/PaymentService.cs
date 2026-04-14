using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PaymentModel = PaymentService.Models.Payment;

namespace PaymentService.Services;

/// <summary>
/// Implements the <see cref="IPaymentService"/> interface using Razorpay as the payment gateway.
/// Handles order creation, signature verification, status queries, webhook processing,
/// and payment history retrieval.
/// Communicates with the Razorpay REST API using HTTP Basic authentication (key:secret).
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>Razorpay publishable key ID, read from configuration.</summary>
    private readonly string _keyId;

    /// <summary>Razorpay secret key used for HMAC signature verification and API authentication.</summary>
    private readonly string _keySecret;

    /// <summary>
    /// Initialises the service, reads Razorpay credentials from configuration,
    /// and configures the HTTP client with Basic authentication headers.
    /// Throws <see cref="InvalidOperationException"/> if Razorpay credentials are not configured.
    /// </summary>
    public PaymentService(PaymentDbContext context, IConfiguration config,
        ILogger<PaymentService> logger, HttpClient httpClient)
    {
        _context = context;
        _config = config;
        _logger = logger;
        _httpClient = httpClient;

        _keyId = _config["Razorpay:KeyId"] ?? throw new InvalidOperationException("Razorpay KeyId not configured");
        _keySecret = _config["Razorpay:KeySecret"] ?? throw new InvalidOperationException("Razorpay KeySecret not configured");

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        _httpClient.BaseAddress = new Uri("https://api.razorpay.com/v1/");
    }

    /// <summary>
    /// Creates a Razorpay order via the Razorpay Orders API and persists a <c>Pending</c> payment record.
    /// Converts the INR amount to paise (×100) as required by Razorpay.
    /// Throws <see cref="InvalidOperationException"/> if a non-failed payment already exists for the order,
    /// or if the Razorpay API call fails.
    /// </summary>
    public async Task<CreatePaymentOrderResponse> CreatePaymentOrderAsync(CreatePaymentOrderRequest request, Guid userId, string? customerName = null, string? customerEmail = null, string? customerPhone = null)
    {
        // Check if payment already exists
        var existing = await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == request.OrderId && p.Status != PaymentStatus.Failed);
        if (existing != null)
            throw new InvalidOperationException("Payment already exists for this order");

        var amountInPaise = (int)(request.Amount * 100);
        var orderData = new
        {
            amount = amountInPaise,
            currency = request.Currency ?? "INR",
            receipt = $"ord_{request.OrderId.ToString()[..8]}_{DateTime.UtcNow:yyyyMMdd}",
            notes = new Dictionary<string, string> { { "order_id", request.OrderId.ToString() } }
        };

        var json = JsonSerializer.Serialize(orderData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("orders", content);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Razorpay error: {Status} - {Error}", response.StatusCode, err);
            throw new InvalidOperationException($"Failed to create Razorpay order: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var razorpayOrder = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var razorpayOrderId = razorpayOrder.GetProperty("id").GetString()!;

        var payment = new PaymentModel
        {
            UserId = userId,
            OrderId = request.OrderId,
            Amount = request.Amount,
            Currency = request.Currency ?? "INR",
            RazorpayOrderId = razorpayOrderId,
            Status = PaymentStatus.Pending
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return new CreatePaymentOrderResponse
        {
            RazorpayOrderId = razorpayOrderId,
            RazorpayKeyId = _keyId,
            Amount = request.Amount,
            Currency = request.Currency ?? "INR",
            OrderId = request.OrderId.ToString(),
            CustomerName = customerName ?? "",
            CustomerEmail = customerEmail ?? "",
            CustomerPhone = customerPhone ?? ""
        };
    }

    /// <summary>
    /// Verifies the Razorpay payment signature using HMAC-SHA256.
    /// The expected signature is computed as <c>HMAC-SHA256(orderId|paymentId, keySecret)</c>.
    /// Updates the payment record to <c>Paid</c> on success or <c>Failed</c> on invalid signature.
    /// Returns a failure response if no matching payment record is found.
    /// </summary>
    public async Task<VerifyPaymentResponse> VerifyPaymentAsync(VerifyPaymentRequest request)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.RazorpayOrderId == request.RazorpayOrderId);

        if (payment == null)
            return new VerifyPaymentResponse { IsValid = false, Status = "Failed", Message = "Payment not found" };

        var isValid = VerifySignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature);
        if (!isValid)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = "Invalid signature";
            payment.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new VerifyPaymentResponse { IsValid = false, Status = "Failed", Message = "Invalid signature" };
        }

        payment.RazorpayPaymentId = request.RazorpayPaymentId;
        payment.RazorpaySignature = request.RazorpaySignature;
        payment.Status = PaymentStatus.Paid;
        payment.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new VerifyPaymentResponse { IsValid = true, Status = "Paid", Message = "Payment verified", PaymentId = payment.Id };
    }

    /// <summary>Returns the payment status for a given internal payment ID, or null if not found.</summary>
    public async Task<PaymentStatusResponse?> GetPaymentStatusAsync(Guid paymentId)
    {
        var p = await _context.Payments.FindAsync(paymentId);
        return p == null ? null : Map(p);
    }

    /// <summary>Returns the payment status for a given Razorpay order ID, or null if not found.</summary>
    public async Task<PaymentStatusResponse?> GetPaymentStatusByOrderIdAsync(string razorpayOrderId)
    {
        var p = await _context.Payments.FirstOrDefaultAsync(x => x.RazorpayOrderId == razorpayOrderId);
        return p == null ? null : Map(p);
    }

    /// <summary>Returns all payments for a user ordered by creation date descending.</summary>
    public async Task<List<PaymentStatusResponse>> GetUserPaymentsAsync(Guid userId)
    {
        var list = await _context.Payments.Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt).ToListAsync();
        return list.Select(Map).ToList();
    }

    /// <summary>
    /// Processes a Razorpay webhook event by updating the corresponding payment record.
    /// Handles <c>payment.captured</c> → Paid and <c>payment.failed</c> → Failed.
    /// Returns false if no payment record matches the webhook's payment ID.
    /// </summary>
    public async Task<bool> HandleWebhookAsync(RazorpayWebhookEvent webhookEvent, string signature)
    {
        var paymentId = webhookEvent.Payload.Payment.Id;
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.RazorpayPaymentId == paymentId);
        if (payment == null) return false;

        if (webhookEvent.Event == "payment.captured")
        {
            payment.Status = PaymentStatus.Paid;
            payment.CompletedAt = DateTime.UtcNow;
        }
        else if (webhookEvent.Event == "payment.failed")
        {
            payment.Status = PaymentStatus.Failed;
            payment.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Verifies a Razorpay payment signature using HMAC-SHA256.
    /// The payload is <c>razorpayOrderId|razorpayPaymentId</c> signed with the Razorpay key secret.
    /// Comparison is case-insensitive to handle hex casing differences.
    /// </summary>
    private bool VerifySignature(string orderId, string paymentId, string signature)
    {
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_keySecret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();
        return signature.Equals(hash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Maps a <see cref="PaymentModel"/> entity to a <see cref="PaymentStatusResponse"/> DTO.</summary>
    private static PaymentStatusResponse Map(PaymentModel p) => new()
    {
        PaymentId = p.Id, OrderId = p.OrderId, Status = p.Status.ToString(), Amount = p.Amount,
        Currency = p.Currency, PaymentMethod = p.PaymentMethod,
        CreatedAt = p.CreatedAt, CompletedAt = p.CompletedAt, FailureReason = p.FailureReason
    };
}
