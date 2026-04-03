using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Backend.Models;

namespace Backend.Services;

public class EmailService(IConfiguration config, ILogger<EmailService> logger)
{
    private readonly string _host     = config["Email:Host"] ?? "";
    private readonly int    _port     = int.Parse(config["Email:Port"] ?? "587");
    private readonly string _user     = config["Email:Username"] ?? "";
    private readonly string _pass     = config["Email:Password"] ?? "";
    private readonly string _from     = config["Email:From"] ?? "";
    private readonly string _fromName = config["Email:FromName"] ?? "FreshMart";
    private readonly string _appUrl   = config["App:Url"] ?? "http://localhost";

    // ── public send methods ──────────────────────────────────────────────────

    public Task SendOrderPlacedAsync(AppUser user, Order order) =>
        SendAsync(user.Email, $"Order Confirmed — #{ShortId(order.Id)}", BuildOrderPlaced(user, order));

    public Task SendOrderProcessingAsync(AppUser user, Order order) =>
        SendAsync(user.Email, $"We're Preparing Your Order #{ShortId(order.Id)}", BuildOrderProcessing(user, order));

    public Task SendOrderShippedAsync(AppUser user, Order order) =>
        SendAsync(user.Email, $"Your Order #{ShortId(order.Id)} Has Been Shipped!", BuildOrderShipped(user, order));

    public Task SendOutForDeliveryAsync(AppUser user, Order order) =>
        SendAsync(user.Email, $"Your Order #{ShortId(order.Id)} Is Out for Delivery", BuildOutForDelivery(user, order));

    public Task SendOrderDeliveredAsync(AppUser user, Order order) =>
        SendAsync(user.Email, $"Your Order #{ShortId(order.Id)} Has Been Delivered!", BuildOrderDelivered(user, order));

    public Task SendOrderCancelledAsync(AppUser user, Order order) =>
        SendAsync(user.Email, $"Your Order #{ShortId(order.Id)} Has Been Cancelled", BuildOrderCancelled(user, order));

    // ── core send ────────────────────────────────────────────────────────────

    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(_host))
        {
            logger.LogWarning("Email not configured – skipping send to {Email}", toEmail);
            return;
        }
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _from));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(_host, _port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_user, _pass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }

    // ── shared helpers ───────────────────────────────────────────────────────

    private static string ShortId(Guid id) => id.ToString()[..8].ToUpper();

    private string TrackButton(Guid orderId) =>
        $"""<a href="{_appUrl}/orders/{orderId}/track" style="display:inline-block;background:#22c55e;color:#fff;text-decoration:none;padding:12px 28px;border-radius:6px;font-weight:bold;font-size:15px;margin:18px 0;">Track Order</a>""";

    private static string ItemsTable(Order order)
    {
        var rows = string.Join("", order.Items.Select(i =>
            $"""
            <tr>
              <td style="padding:10px 12px;border-bottom:1px solid #e5e7eb;">{i.ProductName}</td>
              <td style="padding:10px 12px;border-bottom:1px solid #e5e7eb;text-align:center;">{i.Quantity}</td>
              <td style="padding:10px 12px;border-bottom:1px solid #e5e7eb;text-align:right;">₹{i.Quantity * i.UnitPrice:F2}</td>
            </tr>
            """));

        return $"""
            <table style="width:100%;border-collapse:collapse;margin:16px 0;">
              <thead>
                <tr style="background:#f9fafb;">
                  <th style="padding:10px 12px;text-align:left;font-size:13px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;">Item</th>
                  <th style="padding:10px 12px;text-align:center;font-size:13px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;">Qty</th>
                  <th style="padding:10px 12px;text-align:right;font-size:13px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;">Price</th>
                </tr>
              </thead>
              <tbody>{rows}</tbody>
            </table>
            <p style="font-size:16px;font-weight:bold;color:#111827;margin:4px 0 20px;">Total: ₹{order.TotalAmount:F2}</p>
            """;
    }

    // ── base layout ──────────────────────────────────────────────────────────

    private static string Layout(string body) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/>
        <title>FreshMart</title></head>
        <body style="margin:0;padding:0;background:#f3f4f6;font-family:Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:32px 0;">
            <tr><td align="center">
              <table width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,0.08);max-width:600px;">
                <tr>
                  <td style="padding:32px 40px;">
                    {body}
                  </td>
                </tr>
                <tr>
                  <td style="background:#f9fafb;padding:16px 40px;border-top:1px solid #e5e7eb;">
                    <p style="margin:0;font-size:12px;color:#9ca3af;text-align:center;">
                      © FreshMart · Fresh groceries delivered to your door<br/>
                      You're receiving this because you placed an order with us.
                    </p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body></html>
        """;

    // ── templates ────────────────────────────────────────────────────────────

    private string BuildOrderPlaced(AppUser u, Order o) => Layout($"""
        <h2 style="color:#16a34a;font-size:22px;margin:0 0 16px;">Order Confirmed ✅</h2>
        <p style="color:#374151;font-size:15px;margin:0 0 20px;">
          Hi {u.FirstName}, your order <strong>#{ShortId(o.Id)}</strong> has been placed successfully.
        </p>
        {ItemsTable(o)}
        {TrackButton(o.Id)}
        <p style="color:#6b7280;font-size:13px;margin:16px 0 0;">Estimated delivery: 2 business days.</p>
        """);

    private string BuildOrderProcessing(AppUser u, Order o) => Layout($"""
        <h2 style="color:#2563eb;font-size:22px;margin:0 0 16px;">Order Processing 🔄</h2>
        <p style="color:#374151;font-size:15px;margin:0 0 20px;">
          Hi {u.FirstName}, your order <strong>#{ShortId(o.Id)}</strong> is being prepared by our team.
        </p>
        {ItemsTable(o)}
        {TrackButton(o.Id)}
        <p style="color:#6b7280;font-size:13px;margin:16px 0 0;">We'll notify you once it's shipped.</p>
        """);

    private string BuildOrderShipped(AppUser u, Order o) => Layout($"""
        <h2 style="color:#7c3aed;font-size:22px;margin:0 0 16px;">Order Shipped 📦</h2>
        <p style="color:#374151;font-size:15px;margin:0 0 20px;">
          Hi {u.FirstName}, your order <strong>#{ShortId(o.Id)}</strong> has been shipped and is on its way!
        </p>
        {ItemsTable(o)}
        {TrackButton(o.Id)}
        <p style="color:#6b7280;font-size:13px;margin:16px 0 0;">
          Estimated delivery: {o.EstimatedDelivery?.ToString("dddd, MMMM dd yyyy") ?? "2 business days"}.
        </p>
        """);

    private string BuildOutForDelivery(AppUser u, Order o) => Layout($"""
        <h2 style="color:#ea580c;font-size:22px;margin:0 0 16px;">Out for Delivery 🚚</h2>
        <p style="color:#374151;font-size:15px;margin:0 0 20px;">
          Hi {u.FirstName}, your order <strong>#{ShortId(o.Id)}</strong> is out for delivery and will arrive today!
        </p>
        {ItemsTable(o)}
        {TrackButton(o.Id)}
        <p style="color:#6b7280;font-size:13px;margin:16px 0 0;">Please be available to receive your package. 📬</p>
        """);

    private string BuildOrderDelivered(AppUser u, Order o) => Layout($"""
        <h2 style="color:#16a34a;font-size:22px;margin:0 0 16px;">Order Delivered 🎉</h2>
        <p style="color:#374151;font-size:15px;margin:0 0 20px;">
          Hi {u.FirstName}, your order <strong>#{ShortId(o.Id)}</strong> has been delivered. Enjoy your fresh groceries!
        </p>
        {ItemsTable(o)}
        <p style="color:#6b7280;font-size:13px;margin:16px 0 0;">
          Delivered on {o.DeliveredAt?.ToString("dddd, MMMM dd yyyy") ?? DateTime.UtcNow.ToString("dddd, MMMM dd yyyy")}.
          Loved it? Leave us a review ⭐
        </p>
        """);

    private string BuildOrderCancelled(AppUser u, Order o) => Layout($"""
        <h2 style="color:#dc2626;font-size:22px;margin:0 0 16px;">Order Cancelled ❌</h2>
        <p style="color:#374151;font-size:15px;margin:0 0 20px;">
          Hi {u.FirstName}, your order <strong>#{ShortId(o.Id)}</strong> has been cancelled.
        </p>
        {ItemsTable(o)}
        <p style="color:#6b7280;font-size:13px;margin:16px 0 0;">
          If you paid online, a refund will be processed within 5–7 business days.<br/>
          Need help? Contact our support team.
        </p>
        """);
}
