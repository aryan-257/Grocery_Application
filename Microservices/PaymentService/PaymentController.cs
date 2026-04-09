using PaymentService.DTOs;
using PaymentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PaymentService.Controllers;

/// <summary>
/// Controller for handling payment operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a payment order with Razorpay
    /// </summary>
    /// <param name="request">Payment order creation request</param>
    /// <returns>Payment order details for frontend integration</returns>
    [HttpPost("create-order")]
    public async Task<ActionResult<CreatePaymentOrderResponse>> CreatePaymentOrder([FromBody] CreatePaymentOrderRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _paymentService.CreatePaymentOrderAsync(request, userId);
            
            _logger.LogInformation("Payment order created for user {UserId}, order {OrderId}", userId, request.OrderId);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for creating payment order");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for creating payment order");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment order");
            return StatusCode(500, new { message = "An error occurred while creating payment order" });
        }
    }

    /// <summary>
    /// Verifies payment after successful payment on frontend
    /// </summary>
    /// <param name="request">Payment verification request</param>
    /// <returns>Payment verification result</returns>
    [HttpPost("verify")]
    public async Task<ActionResult<VerifyPaymentResponse>> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        try
        {
            var response = await _paymentService.VerifyPaymentAsync(request);
            
            if (response.IsValid)
            {
                _logger.LogInformation("Payment verified successfully for order {OrderId}", request.RazorpayOrderId);
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("Payment verification failed for order {OrderId}: {Message}", 
                    request.RazorpayOrderId, response.Message);
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment for order {OrderId}", request.RazorpayOrderId);
            return StatusCode(500, new { message = "An error occurred while verifying payment" });
        }
    }

    /// <summary>
    /// Gets payment status by payment ID
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <returns>Payment status details</returns>
    [HttpGet("{paymentId}/status")]
    public async Task<ActionResult<PaymentStatusResponse>> GetPaymentStatus(Guid paymentId)
    {
        try
        {
            var response = await _paymentService.GetPaymentStatusAsync(paymentId);
            
            if (response == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for {PaymentId}", paymentId);
            return StatusCode(500, new { message = "An error occurred while getting payment status" });
        }
    }

    /// <summary>
    /// Gets payment status by Razorpay order ID
    /// </summary>
    /// <param name="razorpayOrderId">Razorpay order ID</param>
    /// <returns>Payment status details</returns>
    [HttpGet("order/{razorpayOrderId}/status")]
    public async Task<ActionResult<PaymentStatusResponse>> GetPaymentStatusByOrderId(string razorpayOrderId)
    {
        try
        {
            var response = await _paymentService.GetPaymentStatusByOrderIdAsync(razorpayOrderId);
            
            if (response == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for order {RazorpayOrderId}", razorpayOrderId);
            return StatusCode(500, new { message = "An error occurred while getting payment status" });
        }
    }

    /// <summary>
    /// Gets all payments for the current user
    /// </summary>
    /// <returns>List of user payments</returns>
    [HttpGet("my-payments")]
    public async Task<ActionResult<List<PaymentStatusResponse>>> GetMyPayments()
    {
        try
        {
            var userId = GetCurrentUserId();
            var payments = await _paymentService.GetUserPaymentsAsync(userId);
            
            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user payments");
            return StatusCode(500, new { message = "An error occurred while getting payments" });
        }
    }

    /// <summary>
    /// Handles Razorpay webhook events
    /// </summary>
    /// <param name="webhookEvent">Webhook event data</param>
    /// <returns>Success/failure response</returns>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook([FromBody] RazorpayWebhookEvent webhookEvent)
    {
        try
        {
            // Get signature from headers
            var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault() ?? string.Empty;
            
            var success = await _paymentService.HandleWebhookAsync(webhookEvent, signature);
            
            if (success)
            {
                _logger.LogInformation("Webhook processed successfully for event {Event}", webhookEvent.Event);
                return Ok(new { message = "Webhook processed successfully" });
            }
            else
            {
                _logger.LogWarning("Failed to process webhook for event {Event}", webhookEvent.Event);
                return BadRequest(new { message = "Failed to process webhook" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, new { message = "An error occurred while processing webhook" });
        }
    }

    /// <summary>
    /// Gets the current user ID from JWT claims
    /// </summary>
    /// <returns>Current user ID</returns>
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