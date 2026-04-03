using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Backend.Services;

/// <summary>
/// Service for handling payment operations with Razorpay
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _keyId;
    private readonly string _keySecret;
    private readonly string _webhookSecret;

    public PaymentService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<PaymentService> logger,
        HttpClient httpClient)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        
        // Get Razorpay configuration
        _keyId = _configuration["Razorpay:KeyId"] ?? throw new InvalidOperationException("Razorpay KeyId not configured");
        _keySecret = _configuration["Razorpay:KeySecret"] ?? throw new InvalidOperationException("Razorpay KeySecret not configured");
        _webhookSecret = _configuration["Razorpay:WebhookSecret"] ?? string.Empty;
        
        // Configure HttpClient for Razorpay API
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        _httpClient.BaseAddress = new Uri("https://api.razorpay.com/v1/");
    }

    /// <summary>
    /// Creates a payment order with Razorpay
    /// </summary>
    public async Task<CreatePaymentOrderResponse> CreatePaymentOrderAsync(CreatePaymentOrderRequest request, Guid userId)
    {
        try
        {
            _logger.LogInformation($"Creating payment order for user {userId}, order {request.OrderId}, amount {request.Amount}");

            // Validate order exists and belongs to user
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == userId);

            if (order == null)
            {
                throw new ArgumentException("Order not found or does not belong to user");
            }

            // Get user details separately
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Check if payment already exists for this order
            var existingPayment = await _context.Set<Backend.Models.Payment>()
                .FirstOrDefaultAsync(p => p.OrderId == request.OrderId && p.Status != PaymentStatus.Failed);

            if (existingPayment != null)
            {
                throw new InvalidOperationException("Payment already exists for this order");
            }

            // Convert amount to paise (Razorpay expects amount in smallest currency unit)
            var amountInPaise = (int)(request.Amount * 100);

            // Create Razorpay order using HTTP API
            var orderData = new
            {
                amount = amountInPaise,
                currency = request.Currency,
                receipt = $"ord_{request.OrderId.ToString()[..8]}_{DateTime.UtcNow:yyyyMMdd}",
                notes = new Dictionary<string, string>
                {
                    {"order_id", request.OrderId.ToString()},
                    {"user_id", userId.ToString()},
                    {"notes", request.Notes ?? string.Empty}
                }
            };

            var json = JsonSerializer.Serialize(orderData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("orders", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Razorpay API error: {response.StatusCode} - {errorContent}");
                throw new InvalidOperationException($"Failed to create Razorpay order: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var razorpayOrder = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var razorpayOrderId = razorpayOrder.GetProperty("id").GetString() ?? throw new InvalidOperationException("Invalid Razorpay response");

            // Create payment record in database
            var payment = new Backend.Models.Payment
            {
                UserId = userId,
                OrderId = request.OrderId,
                Amount = request.Amount,
                Currency = request.Currency,
                RazorpayOrderId = razorpayOrderId,
                Status = PaymentStatus.Pending
            };

            _context.Set<Backend.Models.Payment>().Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Payment order created successfully. PaymentId: {payment.Id}, RazorpayOrderId: {razorpayOrderId}");

            // Return response for frontend
            return new CreatePaymentOrderResponse
            {
                RazorpayOrderId = razorpayOrderId,
                RazorpayKeyId = _keyId,
                Amount = request.Amount,
                Currency = request.Currency,
                OrderId = request.OrderId.ToString(),
                CustomerName = $"{user.FirstName} {user.LastName}".Trim(),
                CustomerEmail = user.Email,
                CustomerPhone = user.PhoneNumber ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating payment order for user {userId}, order {request.OrderId}");
            throw;
        }
    }

    /// <summary>
    /// Verifies payment signature from Razorpay
    /// </summary>
    public async Task<VerifyPaymentResponse> VerifyPaymentAsync(VerifyPaymentRequest request)
    {
        try
        {
            _logger.LogInformation($"Verifying payment. RazorpayOrderId: {request.RazorpayOrderId}, PaymentId: {request.RazorpayPaymentId}");

            // Find payment record
            var payment = await _context.Set<Backend.Models.Payment>()
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.RazorpayOrderId == request.RazorpayOrderId);

            if (payment == null)
            {
                _logger.LogWarning($"Payment not found for RazorpayOrderId: {request.RazorpayOrderId}");
                return new VerifyPaymentResponse
                {
                    IsValid = false,
                    Status = "Failed",
                    Message = "Payment record not found"
                };
            }

            // Verify signature
            var isValidSignature = VerifyRazorpaySignature(
                request.RazorpayOrderId,
                request.RazorpayPaymentId,
                request.RazorpaySignature);

            if (!isValidSignature)
            {
                _logger.LogWarning($"Invalid payment signature for RazorpayOrderId: {request.RazorpayOrderId}");
                
                // Update payment status to failed
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = "Invalid signature";
                payment.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new VerifyPaymentResponse
                {
                    IsValid = false,
                    Status = "Failed",
                    Message = "Invalid payment signature"
                };
            }

            // Update payment record with success details
            payment.RazorpayPaymentId = request.RazorpayPaymentId;
            payment.RazorpaySignature = request.RazorpaySignature;
            payment.Status = PaymentStatus.Paid;
            payment.CompletedAt = DateTime.UtcNow;

            // Update order status to paid
            payment.Order.Status = "Paid";

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Payment verified successfully. PaymentId: {payment.Id}");

            return new VerifyPaymentResponse
            {
                IsValid = true,
                Status = "Paid",
                Message = "Payment verified successfully",
                PaymentId = payment.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error verifying payment for RazorpayOrderId: {request.RazorpayOrderId}");
            throw;
        }
    }

    /// <summary>
    /// Gets payment status by payment ID
    /// </summary>
    public async Task<PaymentStatusResponse?> GetPaymentStatusAsync(Guid paymentId)
    {
        var payment = await _context.Set<Backend.Models.Payment>()
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        return payment == null ? null : MapToPaymentStatusResponse(payment);
    }

    /// <summary>
    /// Gets payment status by Razorpay order ID
    /// </summary>
    public async Task<PaymentStatusResponse?> GetPaymentStatusByOrderIdAsync(string razorpayOrderId)
    {
        var payment = await _context.Set<Backend.Models.Payment>()
            .FirstOrDefaultAsync(p => p.RazorpayOrderId == razorpayOrderId);

        return payment == null ? null : MapToPaymentStatusResponse(payment);
    }

    /// <summary>
    /// Handles Razorpay webhook events
    /// </summary>
    public async Task<bool> HandleWebhookAsync(RazorpayWebhookEvent webhookEvent, string signature)
    {
        try
        {
            _logger.LogInformation($"Processing webhook event: {webhookEvent.Event} for payment: {webhookEvent.Payload.Payment.Id}");

            var razorpayPaymentId = webhookEvent.Payload.Payment.Id;
            var payment = await _context.Set<Backend.Models.Payment>()
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.RazorpayPaymentId == razorpayPaymentId);

            if (payment == null)
            {
                _logger.LogWarning($"Payment not found for webhook PaymentId: {razorpayPaymentId}");
                return false;
            }

            // Handle different webhook events
            switch (webhookEvent.Event)
            {
                case "payment.captured":
                    payment.Status = PaymentStatus.Paid;
                    payment.PaymentMethod = webhookEvent.Payload.Payment.Method;
                    payment.CompletedAt = DateTime.UtcNow;
                    payment.Order.Status = "Paid";
                    break;

                case "payment.failed":
                    payment.Status = PaymentStatus.Failed;
                    payment.FailureReason = "Payment failed via webhook";
                    payment.CompletedAt = DateTime.UtcNow;
                    break;

                default:
                    _logger.LogInformation($"Unhandled webhook event: {webhookEvent.Event}");
                    return true;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Webhook processed successfully for payment: {payment.Id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing webhook event: {webhookEvent.Event}");
            return false;
        }
    }

    /// <summary>
    /// Gets all payments for a user
    /// </summary>
    public async Task<List<PaymentStatusResponse>> GetUserPaymentsAsync(Guid userId)
    {
        var payments = await _context.Set<Backend.Models.Payment>()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return payments.Select(MapToPaymentStatusResponse).ToList();
    }

    /// <summary>
    /// Verifies Razorpay payment signature using HMAC SHA256
    /// </summary>
    private bool VerifyRazorpaySignature(string orderId, string paymentId, string signature)
    {
        try
        {
            var payload = $"{orderId}|{paymentId}";
            var expectedSignature = ComputeHmacSha256(payload, _keySecret);
            return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Razorpay signature");
            return false;
        }
    }

    /// <summary>
    /// Computes HMAC SHA256 hash
    /// </summary>
    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    /// <summary>
    /// Maps Payment entity to PaymentStatusResponse DTO
    /// </summary>
    private static PaymentStatusResponse MapToPaymentStatusResponse(Backend.Models.Payment payment)
    {
        return new PaymentStatusResponse
        {
            PaymentId = payment.Id,
            Status = payment.Status.ToString(),
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod,
            CreatedAt = payment.CreatedAt,
            CompletedAt = payment.CompletedAt,
            FailureReason = payment.FailureReason
        };
    }
}