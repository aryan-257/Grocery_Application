namespace AuthService.Models;

/// <summary>
/// Represents a registered user in the FreshMart platform.
/// Stores credentials, profile data, role assignment, and OAuth linkage.
/// Used by AuthService for authentication, authorization, and token management.
/// </summary>
public class AppUser
{
    /// <summary>Unique identifier for the user (primary key).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>User's email address. Used as the login credential. Must be unique across all users.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt-hashed password. Never stored or transmitted in plain text.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>User's given (first) name. Embedded in JWT claims as <c>given_name</c>.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>User's family (last) name. Embedded in JWT claims as <c>family_name</c>.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Role assigned to the user. Controls access across all microservices.
    /// Valid values: <c>Admin</c>, <c>StoreManager</c>, <c>DeliveryDriver</c>, <c>Customer</c>.
    /// Defaults to <c>Customer</c> on registration.
    /// </summary>
    public string Role { get; set; } = "Customer";

    /// <summary>Optional phone number for contact and delivery coordination.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>UTC timestamp of when the account was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates whether the account is active. Inactive users cannot log in.
    /// Admins can toggle this via the Users management API.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Opaque refresh token used to obtain new access tokens without re-authentication.
    /// Rotated on every refresh. Cleared on logout.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>UTC expiry time for the current refresh token. Tokens past this date are rejected.</summary>
    public DateTime? RefreshTokenExpiry { get; set; }

    /// <summary>
    /// Google OAuth2 subject identifier (<c>sub</c> claim from Google's userinfo endpoint).
    /// Set when the user authenticates via Google. Used to link Google accounts to local users.
    /// </summary>
    public string? GoogleId { get; set; }
}
