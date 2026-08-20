namespace AuthService.Core.Models;

/// <summary>
/// This is the main user model for the application.
/// It stores login credentials, profile info, and the refresh token for JWT.
/// </summary>
public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    /// <summary>Password is stored as a BCrypt hash, never as plain text.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Role controls what the user can access.
    /// Possible values: Admin, StoreManager, Customer.
    /// Default is Customer when a new user registers.
    /// </summary>
    public string Role { get; set; } = "Customer";

    public string? PhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>If false, the user cannot login. Admin can block/unblock users.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Used for refresh token functionality.
    /// When user logs in, a refresh token is saved here so they don't have to
    /// login again after the access token expires.
    /// </summary>
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    public string? GoogleId { get; set; }
}
