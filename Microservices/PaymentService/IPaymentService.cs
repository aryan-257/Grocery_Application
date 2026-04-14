using PaymentService.DTOs;
using PaymentService.Models;

namespace PaymentService.Services;

/// <summary>
/// Defines the contract for payment operations in the FreshMart PaymentService.
/// Abstracts Razorpay integration so the controller and tests are decoupled from the implementation.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a Razorpay payment order for a given FreshMart order.
    /// Persists a <c>Pending</c> payment record and returns the Razorpay order details
    /// needed by the frontend to initialise the Razorpay checkout modal.
    /// Throws <see cref="InvalidOperationException"/> if a non-failed payment already exists for the order.
    /// </summary>
    /// <param name="request">Payment order creation request containing order ID, amount, and currency.</param>
    /// <param name="userId">ID of the user initiating the payment.</param>
    /// <param name="customerName">Optional customer name passed to Razorpay for prefilling the checkout form.</param>
    /// <param name="customerEmail">Optional customer email passed to Razorpay.</param>
    /// <param name="customerPhone">Optional customer phone passed to Razorpay.</param>
    /// <returns>Response containing the Razorpay order ID, key, and amount for frontend integration.</returns>
    Task<CreatePaymentOrderResponse> CreatePaymentOrderAsync(CreatePaymentOrderRequest request, Guid userId, string? customerName = null, string? customerEmail = null, string? customerPhone = null);

    /// <summary>
    /// Verifies the Razorpay payment signature after the customer completes payment on the frontend.
    /// On valid signature, marks the payment as <c>Paid</c>.
    /// On invalid signature, marks the payment as <c>Failed</c>.
    /// </summary>
    /// <param name="request">Verification request containing the Razorpay order ID, payment ID, and signature.</param>
    /// <returns>Verification result indicating success or failure with a descriptive message.</returns>
    Task<VerifyPaymentResponse> VerifyPaymentAsync(VerifyPaymentRequest request);

    /// <summary>
    /// Retrieves the current status of a payment by its internal FreshMart payment ID.
    /// Returns null if no payment with the given ID exists.
    /// </summary>
    /// <param name="paymentId">The internal FreshMart payment record ID.</param>
    /// <returns>Payment status details, or null if not found.</returns>
    Task<PaymentStatusResponse?> GetPaymentStatusAsync(Guid paymentId);

    /// <summary>
    /// Retrieves the current status of a payment by its Razorpay order ID.
    /// Useful for looking up payment status from the frontend after checkout.
    /// Returns null if no matching payment exists.
    /// </summary>
    /// <param name="razorpayOrderId">The Razorpay-generated order ID (e.g., <c>order_XXXX</c>).</param>
    /// <returns>Payment status details, or null if not found.</returns>
    Task<PaymentStatusResponse?> GetPaymentStatusByOrderIdAsync(string razorpayOrderId);

    /// <summary>
    /// Processes a Razorpay webhook event to update payment status asynchronously.
    /// Handles <c>payment.captured</c> (marks as Paid) and <c>payment.failed</c> (marks as Failed).
    /// Returns false if no matching payment record is found.
    /// </summary>
    /// <param name="webhookEvent">The deserialized Razorpay webhook event payload.</param>
    /// <param name="signature">The <c>X-Razorpay-Signature</c> header value for verification.</param>
    /// <returns>True if the webhook was processed successfully; false otherwise.</returns>
    Task<bool> HandleWebhookAsync(RazorpayWebhookEvent webhookEvent, string signature);

    /// <summary>
    /// Returns all payment records for a specific user, ordered by creation date descending.
    /// Used to populate the user's payment history page.
    /// </summary>
    /// <param name="userId">The user whose payment history to retrieve.</param>
    /// <returns>List of payment status responses for the user.</returns>
    Task<List<PaymentStatusResponse>> GetUserPaymentsAsync(Guid userId);
}
