namespace OrderService.Core.Models;

/// <summary>
/// Local copy of user data inside OrderService.
/// We save this when an order is created so we don't have to call
/// AuthService every time we need the customer's name or email.
/// </summary>
public class AppUser
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}
