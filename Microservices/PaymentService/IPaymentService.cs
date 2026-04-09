using PaymentService.DTOs;
using PaymentService.Models;

namespace PaymentService.Services;

/// <summary>
/// Interface for payment service operations
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a payment order with Razorpay
    /// </summary>
    /// <param name="request">Payment order creation request</param>
    /// <param name="userId">ID of the user making the payment</param>
    /// <returns>Payment order response with Razorpay details</returns>
    Task<CreatePaymentOrderResponse> CreatePaymentOrderAsync(CreatePaymentOrderRequest request, Guid userId, string? customerName = null, string? customerEmail = null, string? customerPhone = null);
    
    /// <summary>
    /// Verifies payment signature from Razorpay
    /// </summary>
    /// <param name="request">Payment verification request</param>
    /// <returns>Payment verification response</returns>
    Task<VerifyPaymentResponse> VerifyPaymentAsync(VerifyPaymentRequest request);
    
    /// <summary>
    /// Gets payment status by payment ID
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <returns>Payment status response</returns>
    Task<PaymentStatusResponse?> GetPaymentStatusAsync(Guid paymentId);
    
    /// <summary>
    /// Gets payment status by Razorpay order ID
    /// </summary>
    /// <param name="razorpayOrderId">Razorpay order ID</param>
    /// <returns>Payment status response</returns>
    Task<PaymentStatusResponse?> GetPaymentStatusByOrderIdAsync(string razorpayOrderId);
    
    /// <summary>
    /// Handles Razorpay webhook events
    /// </summary>
    /// <param name="webhookEvent">Webhook event data</param>
    /// <param name="signature">Webhook signature for verification</param>
    /// <returns>True if webhook was processed successfully</returns>
    Task<bool> HandleWebhookAsync(RazorpayWebhookEvent webhookEvent, string signature);
    
    /// <summary>
    /// Gets all payments for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of payment status responses</returns>
    Task<List<PaymentStatusResponse>> GetUserPaymentsAsync(Guid userId);
}