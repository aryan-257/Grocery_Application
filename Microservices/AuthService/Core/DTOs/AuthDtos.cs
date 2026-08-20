namespace AuthService.Core.DTOs;

/// <summary>Data required to register a new user.</summary>
public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber);

public record LoginRequest(string Email, string Password);

/// <summary>Returned after successful login or register. Contains the JWT access token and refresh token.</summary>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string ExpiresAt,
    string Role,
    string UserId);

/// <summary>User profile returned when calling GET /auth/me.</summary>
public record UserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string? PhoneNumber);

public record UserAdminDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string? PhoneNumber,
    bool IsActive,
    DateTime CreatedAt);

public record UpdateProfileRequest(string FirstName, string LastName, string? PhoneNumber);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UpdateUserRequest(
    string? Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber);

public record ChangeRoleRequest(string Role);
