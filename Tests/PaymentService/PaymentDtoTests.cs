using NUnit.Framework;
using System.ComponentModel.DataAnnotations;

namespace FreshMart.Tests.PaymentService;

// Inline DTOs
public class CreatePaymentOrderRequest
{
    [Required] public Guid OrderId { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, double.MaxValue)] public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string? Notes { get; set; }
}

public class VerifyPaymentRequest
{
    [Required] public string RazorpayOrderId { get; set; } = "";
    [Required] public string RazorpayPaymentId { get; set; } = "";
    [Required] public string RazorpaySignature { get; set; } = "";
}

public class VerifyPaymentResponse
{
    public bool IsValid { get; set; }
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public Guid? PaymentId { get; set; }
}

// Inline signature verification logic
public static class RazorpayHelper
{
    public static bool VerifySignature(string orderId, string paymentId, string signature, string secret)
    {
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        var expected = BitConverter.ToString(hash).Replace("-", "").ToLower();
        return expected == signature;
    }

    public static decimal PaisaToRupees(long paisa) => paisa / 100m;
    public static long RupeesToPaisa(decimal rupees) => (long)(rupees * 100);
}

[TestFixture]
public class PaymentDtoTests
{
    [Test]
    public void CreatePaymentOrderRequest_DefaultCurrency_IsINR()
    {
        var req = new CreatePaymentOrderRequest { OrderId = Guid.NewGuid(), Amount = 100 };
        Assert.That(req.Currency, Is.EqualTo("INR"));
    }

    [Test]
    public void CreatePaymentOrderRequest_AmountValidation_ZeroFails()
    {
        var req = new CreatePaymentOrderRequest { OrderId = Guid.NewGuid(), Amount = 0 };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(req, new ValidationContext(req), results, true);
        Assert.That(valid, Is.False);
    }

    [Test]
    public void CreatePaymentOrderRequest_AmountValidation_PositivePasses()
    {
        var req = new CreatePaymentOrderRequest { OrderId = Guid.NewGuid(), Amount = 299.50m };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(req, new ValidationContext(req), results, true);
        Assert.That(valid, Is.True);
    }

    [Test]
    public void RazorpayHelper_PaisaToRupees_IsCorrect()
    {
        Assert.That(RazorpayHelper.PaisaToRupees(29950), Is.EqualTo(299.50m));
    }

    [Test]
    public void RazorpayHelper_RupeesToPaisa_IsCorrect()
    {
        Assert.That(RazorpayHelper.RupeesToPaisa(299.50m), Is.EqualTo(29950L));
    }

    [Test]
    public void RazorpayHelper_RupeesToPaisa_SmallAmount()
    {
        Assert.That(RazorpayHelper.RupeesToPaisa(1m), Is.EqualTo(100L));
    }

    [Test]
    public void VerifyPaymentResponse_DefaultIsInvalid()
    {
        var resp = new VerifyPaymentResponse();
        Assert.That(resp.IsValid, Is.False);
    }

    [Test]
    public void VerifyPaymentResponse_CanSetValid()
    {
        var resp = new VerifyPaymentResponse { IsValid = true, Status = "captured", Message = "Payment verified" };
        Assert.That(resp.IsValid, Is.True);
        Assert.That(resp.Status, Is.EqualTo("captured"));
    }

    [Test]
    public void VerifySignature_WrongSecret_ReturnsFalse()
    {
        var result = RazorpayHelper.VerifySignature("order_123", "pay_456", "invalidsig", "wrongsecret");
        Assert.That(result, Is.False);
    }

    [Test]
    public void VerifySignature_CorrectSignature_ReturnsTrue()
    {
        const string secret = "testsecret";
        const string orderId = "order_123";
        const string paymentId = "pay_456";
        var payload = $"{orderId}|{paymentId}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        var sig = BitConverter.ToString(hash).Replace("-", "").ToLower();

        Assert.That(RazorpayHelper.VerifySignature(orderId, paymentId, sig, secret), Is.True);
    }
}
