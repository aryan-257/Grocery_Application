using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PaymentModel = PaymentService.Models.Payment;

namespace PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _keyId;
    private readonly string _keySecret;

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

    public async Task<PaymentStatusResponse?> GetPaymentStatusAsync(Guid paymentId)
    {
        var p = await _context.Payments.FindAsync(paymentId);
        return p == null ? null : Map(p);
    }

    public async Task<PaymentStatusResponse?> GetPaymentStatusByOrderIdAsync(string razorpayOrderId)
    {
        var p = await _context.Payments.FirstOrDefaultAsync(x => x.RazorpayOrderId == razorpayOrderId);
        return p == null ? null : Map(p);
    }

    public async Task<List<PaymentStatusResponse>> GetUserPaymentsAsync(Guid userId)
    {
        var list = await _context.Payments.Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt).ToListAsync();
        return list.Select(Map).ToList();
    }

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

    private bool VerifySignature(string orderId, string paymentId, string signature)
    {
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_keySecret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();
        return signature.Equals(hash, StringComparison.OrdinalIgnoreCase);
    }

    private static PaymentStatusResponse Map(PaymentModel p) => new()
    {
        PaymentId = p.Id, Status = p.Status.ToString(), Amount = p.Amount,
        Currency = p.Currency, PaymentMethod = p.PaymentMethod,
        CreatedAt = p.CreatedAt, CompletedAt = p.CompletedAt, FailureReason = p.FailureReason
    };
}
