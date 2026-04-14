namespace AuthService.DTOs;

/// <summary>Request body for registering a new customer account.</summary>
/// <param name="Email">Email address used as the login credential. Must be unique.</param>
/// <param name="Password">Plain-text password. Will be hashed with BCrypt before storage.</param>
/// <param name="FirstName">User's given name.</param>
/// <param name="LastName">User's family name.</param>
/// <param name="PhoneNumber">Optional contact phone number.</param>
public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? PhoneNumber);

/// <summary>Request body for authenticating with email and password.</summary>
/// <param name="Email">Registered email address.</param>
/// <param name="Password">Plain-text password to verify against the stored hash.</param>
public record LoginRequest(string Email, string Password);

/// <summary>Request body for obtaining a new access token using a refresh token.</summary>
/// <param name="RefreshToken">The opaque refresh token previously issued on login or refresh.</param>
public record RefreshRequest(string RefreshToken);

/// <summary>
/// Response returned on successful login, registration, or token refresh.
/// Contains both the short-lived access token and the long-lived refresh token.
/// </summary>
/// <param name="AccessToken">JWT access token. Valid for the duration specified in <c>ExpiresAt</c>.</param>
/// <param name="RefreshToken">Opaque refresh token. Valid for 7 days. Used to silently renew the access token.</param>
/// <param name="ExpiresAt">ISO 8601 UTC timestamp indicating when the access token expires.</param>
/// <param name="Role">The authenticated user's role (e.g., <c>Admin</c>, <c>Customer</c>).</param>
/// <param name="UserId">The authenticated user's unique identifier.</param>
public record AuthResponse(string AccessToken, string RefreshToken, string ExpiresAt, string Role, string UserId);

/// <summary>Lightweight user profile DTO returned to the authenticated user via <c>GET /me</c>.</summary>
/// <param name="Id">User's unique identifier.</param>
/// <param name="Email">User's email address.</param>
/// <param name="FirstName">User's given name.</param>
/// <param name="LastName">User's family name.</param>
/// <param name="Role">User's assigned role.</param>
/// <param name="PhoneNumber">Optional phone number.</param>
public record UserDto(string Id, string Email, string FirstName, string LastName, string Role, string? PhoneNumber);

/// <summary>Request body for updating the authenticated user's own profile information.</summary>
/// <param name="FirstName">Updated given name.</param>
/// <param name="LastName">Updated family name.</param>
/// <param name="PhoneNumber">Updated phone number (nullable to allow removal).</param>
public record UpdateProfileRequest(string FirstName, string LastName, string? PhoneNumber);

/// <summary>Request body for changing the authenticated user's password.</summary>
/// <param name="CurrentPassword">The user's existing password for verification before the change is applied.</param>
/// <param name="NewPassword">The new plain-text password. Will be hashed before storage.</param>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Extended user DTO returned to Admin users via the Users management API.</summary>
/// <param name="Id">User's unique identifier.</param>
/// <param name="Email">User's email address.</param>
/// <param name="FirstName">User's given name.</param>
/// <param name="LastName">User's family name.</param>
/// <param name="Role">User's assigned role.</param>
/// <param name="PhoneNumber">Optional phone number.</param>
/// <param name="IsActive">Whether the account is currently active.</param>
/// <param name="CreatedAt">UTC timestamp of account creation.</param>
public record UserAdminDto(string Id, string Email, string FirstName, string LastName, string Role, string? PhoneNumber, bool IsActive, DateTime CreatedAt);

/// <summary>Request body for an Admin to update another user's profile fields. All fields are optional.</summary>
/// <param name="Email">New email address. Checked for uniqueness before applying.</param>
/// <param name="FirstName">New given name.</param>
/// <param name="LastName">New family name.</param>
/// <param name="PhoneNumber">New phone number.</param>
public record UpdateUserRequest(string? Email, string? FirstName, string? LastName, string? PhoneNumber);

/// <summary>Request body for an Admin to change a user's role.</summary>
/// <param name="Role">Target role. Must be one of: <c>Admin</c>, <c>StoreManager</c>, <c>DeliveryDriver</c>, <c>Customer</c>.</param>
public record ChangeRoleRequest(string Role);

/// <summary>Request body for Google OAuth2 sign-in. Carries the token returned by the Google Identity SDK.</summary>
/// <param name="IdToken">Google OAuth2 access token or ID token. Verified against Google's userinfo endpoint.</param>
public record GoogleAuthRequest(string IdToken);
