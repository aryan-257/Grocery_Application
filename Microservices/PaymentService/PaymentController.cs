using PaymentService.DTOs;
using PaymentService.Services;
using MassTransit;
using SharedModels.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PaymentService.Controllers;

/// <summary>
/// Handles all payment operations via Razorpay integration.
/// Publishes PaymentCompletedEvent to RabbitMQ when a webhook confirms payment capture,
/// allowing OrderService to transition the order status asynchronously.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PaymentController(
    IPaymentService paymentService,
    ILogger<PaymentController> logger,
    IPublishEndpoint? publishEndpoint = null) : ControllerBase
{

    /// <summary>
    /// Creates a Razorpay payment order for a FreshMart order.
    /// Called by OrderService immediately after order creation to obtain the Razorpay order ID
    /// and key needed by the frontend to launch the Razorpay checkout modal.
    /// Returns 409 Conflict if a non-failed payment already exists for the order.
    /// Accessible by: authenticated users (called server-to-server from OrderService).
    /// </summary>
    [HttpPost("create-order")]
    public async Task<ActionResult<CreatePaymentOrderResponse>> CreatePaymentOrder([FromBody] CreatePaymentOrderRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await paymentService.CreatePaymentOrderAsync(request, userId);

            logger.LogInformation("Payment order created for user {UserId}, order {OrderId}", userId, request.OrderId);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid request for creating payment order");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation for creating payment order");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating payment order");
            return StatusCode(500, new { message = "An error occurred while creating payment order" });
        }
    }

    /// <summary>
    /// Verifies a Razorpay payment after the customer completes checkout on the frontend.
    /// Validates the HMAC-SHA256 signature to confirm the payment is authentic.
    /// Marks the payment as <c>Paid</c> on success or <c>Failed</c> on invalid signature.
    /// Returns 400 if verification fails.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpPost("verify")]
    public async Task<ActionResult<VerifyPaymentResponse>> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        try
        {
            var response = await paymentService.VerifyPaymentAsync(request);

            if (response.IsValid)
            {
                logger.LogInformation("Payment verified successfully for order {OrderId}", request.RazorpayOrderId);
                return Ok(response);
            }
            else
            {
                logger.LogWarning("Payment verification failed for order {OrderId}: {Message}",
                    request.RazorpayOrderId, response.Message);
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying payment for order {OrderId}", request.RazorpayOrderId);
            return StatusCode(500, new { message = "An error occurred while verifying payment" });
        }
    }

    /// <summary>
    /// Returns the current status of a payment by its internal FreshMart payment ID.
    /// Returns 404 if no payment with the given ID exists.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpGet("{paymentId}/status")]
    public async Task<ActionResult<PaymentStatusResponse>> GetPaymentStatus(Guid paymentId)
    {
        try
        {
            var response = await paymentService.GetPaymentStatusAsync(paymentId);

            if (response == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting payment status for {PaymentId}", paymentId);
            return StatusCode(500, new { message = "An error occurred while getting payment status" });
        }
    }

    /// <summary>
    /// Returns the current status of a payment by its Razorpay order ID.
    /// Useful for the frontend to poll payment status after the Razorpay modal closes.
    /// Returns 404 if no matching payment exists.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpGet("order/{razorpayOrderId}/status")]
    public async Task<ActionResult<PaymentStatusResponse>> GetPaymentStatusByOrderId(string razorpayOrderId)
    {
        try
        {
            var response = await paymentService.GetPaymentStatusByOrderIdAsync(razorpayOrderId);

            if (response == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting payment status for order {RazorpayOrderId}", razorpayOrderId);
            return StatusCode(500, new { message = "An error occurred while getting payment status" });
        }
    }

    /// <summary>
    /// Returns all payment records for the currently authenticated user, ordered by date descending.
    /// Used to populate the user's payment history page.
    /// Accessible by: authenticated users.
    /// </summary>
    [HttpGet("my-payments")]
    public async Task<ActionResult<List<PaymentStatusResponse>>> GetMyPayments()
    {
        try
        {
            var userId = GetCurrentUserId();
            var payments = await paymentService.GetUserPaymentsAsync(userId);

            return Ok(payments);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user payments");
            return StatusCode(500, new { message = "An error occurred while getting payments" });
        }
    }

    /// <summary>
    /// Receives and processes Razorpay webhook events (e.g., <c>payment.captured</c>, <c>payment.failed</c>).
    /// The <c>X-Razorpay-Signature</c> header is extracted and passed to the service for verification.
    /// This endpoint is intentionally anonymous — Razorpay cannot send a JWT.
    /// Accessible by: anonymous (Razorpay webhook delivery).
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook([FromBody] RazorpayWebhookEvent webhookEvent)
    {
        try
        {
            var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault() ?? string.Empty;
            var success = await paymentService.HandleWebhookAsync(webhookEvent, signature);

            if (success && webhookEvent.Event == "payment.captured" && publishEndpoint != null)
            {
                // Publish PaymentCompletedEvent — OrderService consumer transitions order to Processing
                try
                {
                    var paymentId = webhookEvent.Payload.Payment.Id;
                    var status = await paymentService.GetPaymentStatusByOrderIdAsync(paymentId);
                    if (status != null)
                    {
                        await publishEndpoint.Publish(new PaymentCompletedEvent(
                            OrderId: status.OrderId,
                            CustomerId: Guid.Empty, // not available in webhook context
                            CustomerEmail: "",
                            Amount: status.Amount));
                        logger.LogInformation("Published PaymentCompletedEvent for Order {OrderId}", status.OrderId);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to publish PaymentCompletedEvent from webhook");
                }
            }

            if (success)
            {
                logger.LogInformation("Webhook processed successfully for event {Event}", webhookEvent.Event);
                return Ok(new { message = "Webhook processed successfully" });
            }

            logger.LogWarning("Failed to process webhook for event {Event}", webhookEvent.Event);
            return BadRequest(new { message = "Failed to process webhook" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, new { message = "An error occurred while processing webhook" });
        }
    }

    /// <summary>
    /// Extracts the authenticated user's ID from the JWT <c>sub</c> claim.
    /// Throws <see cref="UnauthorizedAccessException"/> if the claim is missing or not a valid GUID.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}
