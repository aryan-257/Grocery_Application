namespace Backend.Models;

/// <summary>
/// Represents a payment transaction in the system
/// </summary>
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Reference to the user who made the payment
    /// </summary>
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    
    /// <summary>
    /// Reference to the order being paid for
    /// </summary>
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    /// <summary>
    /// Payment amount in the smallest currency unit (paise for INR)
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Currency code (e.g., INR, USD)
    /// </summary>
    public string Currency { get; set; } = "INR";
    
    /// <summary>
    /// Razorpay order ID returned from create order API
    /// </summary>
    public string RazorpayOrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Razorpay payment ID returned after successful payment
    /// </summary>
    public string? RazorpayPaymentId { get; set; }
    
    /// <summary>
    /// Razorpay signature for payment verification
    /// </summary>
    public string? RazorpaySignature { get; set; }
    
    /// <summary>
    /// Payment status: Pending, Paid, Failed, Refunded
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    /// <summary>
    /// Payment method used (card, netbanking, upi, etc.)
    /// </summary>
    public string? PaymentMethod { get; set; }
    
    /// <summary>
    /// Failure reason if payment failed
    /// </summary>
    public string? FailureReason { get; set; }
    
    /// <summary>
    /// When the payment record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the payment was completed/failed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Additional metadata from Razorpay
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Payment status enumeration
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3,
    Cancelled = 4
}