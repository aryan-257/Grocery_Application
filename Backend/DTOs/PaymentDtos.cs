using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

/// <summary>
/// Request DTO for creating a payment order
/// </summary>
public class CreatePaymentOrderRequest
{
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    public string Currency { get; set; } = "INR";
    
    public string? Notes { get; set; }
}

/// <summary>
/// Response DTO for payment order creation
/// </summary>
public class CreatePaymentOrderResponse
{
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayKeyId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for payment verification
/// </summary>
public class VerifyPaymentRequest
{
    [Required]
    public string RazorpayOrderId { get; set; } = string.Empty;
    
    [Required]
    public string RazorpayPaymentId { get; set; } = string.Empty;
    
    [Required]
    public string RazorpaySignature { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for payment verification
/// </summary>
public class VerifyPaymentResponse
{
    public bool IsValid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
}

/// <summary>
/// DTO for payment status response
/// </summary>
public class PaymentStatusResponse
{
    public Guid PaymentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}

/// <summary>
/// DTO for Razorpay webhook events
/// </summary>
public class RazorpayWebhookEvent
{
    public string Entity { get; set; } = string.Empty;
    public string Account_Id { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public bool Contains { get; set; }
    public RazorpayWebhookPayload Payload { get; set; } = new();
    public long Created_At { get; set; }
}

/// <summary>
/// DTO for Razorpay webhook payload
/// </summary>
public class RazorpayWebhookPayload
{
    public RazorpayWebhookPayment Payment { get; set; } = new();
}

/// <summary>
/// DTO for Razorpay webhook payment data
/// </summary>
public class RazorpayWebhookPayment
{
    public string Entity { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Order_Id { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public long Created_At { get; set; }
}