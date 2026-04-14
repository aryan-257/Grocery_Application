namespace PaymentService.Models;

/// <summary>
/// Represents a payment transaction in the FreshMart platform.
/// Tracks the full lifecycle of a Razorpay payment from creation through verification or failure.
/// One payment record is created per order when the customer initiates checkout.
/// </summary>
public class Payment
{
    /// <summary>Unique identifier for the payment record (primary key).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ID of the customer who initiated the payment.</summary>
    public Guid UserId { get; set; }

    /// <summary>ID of the FreshMart order this payment is associated with.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Payment amount in INR. Stored as a decimal for precision.</summary>
    public decimal Amount { get; set; }

    /// <summary>Currency code. Always <c>INR</c> for FreshMart.</summary>
    public string Currency { get; set; } = "INR";

    /// <summary>
    /// Razorpay-generated order ID (e.g., <c>order_XXXX</c>).
    /// Created when the payment order is initialised with Razorpay.
    /// Used to correlate Razorpay events back to this payment record.
    /// </summary>
    public string RazorpayOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Razorpay payment ID returned after the customer completes payment on the frontend.
    /// Null until the payment is verified.
    /// </summary>
    public string? RazorpayPaymentId { get; set; }

    /// <summary>
    /// HMAC-SHA256 signature from Razorpay used to verify payment authenticity.
    /// Stored after successful verification. Null until verified.
    /// </summary>
    public string? RazorpaySignature { get; set; }

    /// <summary>Current status of the payment. Transitions: Pending → Paid | Failed | Cancelled | Refunded.</summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Payment method used (e.g., <c>card</c>, <c>upi</c>, <c>netbanking</c>). Set by Razorpay.</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>Human-readable reason for payment failure. Populated when <see cref="Status"/> is <c>Failed</c>.</summary>
    public string? FailureReason { get; set; }

    /// <summary>UTC timestamp of when the payment record was created (i.e., when the Razorpay order was initiated).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of when the payment reached a terminal state (Paid, Failed, Refunded, or Cancelled).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Optional JSON metadata for storing additional Razorpay response fields or debugging info.</summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Represents the lifecycle states of a payment transaction.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Payment order created but the customer has not yet completed payment.</summary>
    Pending = 0,

    /// <summary>Payment successfully captured and verified via Razorpay signature.</summary>
    Paid = 1,

    /// <summary>Payment attempt failed (e.g., card declined, timeout).</summary>
    Failed = 2,

    /// <summary>Payment was refunded after a successful capture.</summary>
    Refunded = 3,

    /// <summary>Payment was cancelled before completion.</summary>
    Cancelled = 4
}
