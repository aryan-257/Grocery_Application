namespace ProductService.Models;

/// <summary>
/// Local user projection — populated from JWT claims, not a cross-service dependency.
/// </summary>
public class AppUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
