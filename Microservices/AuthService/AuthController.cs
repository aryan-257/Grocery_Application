using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Http.Json;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

/// <summary>
/// Handles all authentication flows for the FreshMart platform:
/// email/password login, registration, token refresh, logout, profile management,
/// password change, and Google OAuth2 sign-in.
/// Accessible by all users (most endpoints are anonymous; profile endpoints require a valid JWT).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthDbContext db, JwtService jwt) : ControllerBase
{
    /// <summary>Extracts the authenticated user's ID from the JWT <c>sub</c> claim.</summary>
    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID claim not found"));

    /// <summary>
    /// Registers a new customer account.
    /// Validates email uniqueness, hashes the password, and persists the user.
    /// Returns the new user's ID, email, and default role (<c>Customer</c>).
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new { error = "Email already registered" });

        var user = new AppUser
        {
            Email = req.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FirstName = req.FirstName,
            LastName = req.LastName,
            PhoneNumber = req.PhoneNumber
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Ok(new { userId = user.Id, email = user.Email, role = user.Role });
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// On success, issues a JWT access token (1 hour) and a refresh token (7 days).
    /// Returns 401 if credentials are invalid or the account does not exist.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid credentials" });

        var accessToken = jwt.GenerateAccessToken(user);
        var refreshToken = jwt.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        return Ok(new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddHours(1).ToString("o"), user.Role, user.Id.ToString()));
    }

    /// <summary>
    /// Issues a new access token and rotates the refresh token.
    /// Validates that the provided refresh token exists in the database and has not expired.
    /// Returns 401 if the token is invalid or expired.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.RefreshToken == req.RefreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);
        if (user == null) return Unauthorized(new { error = "Invalid or expired refresh token" });

        var accessToken = jwt.GenerateAccessToken(user);
        var refreshToken = jwt.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        return Ok(new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddHours(1).ToString("o"), user.Role, user.Id.ToString()));
    }

    /// <summary>
    /// Logs out the authenticated user by invalidating their refresh token.
    /// After logout, the refresh token cannot be used to obtain new access tokens.
    /// Requires a valid JWT. Returns 204 No Content on success.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user != null) { user.RefreshToken = null; await db.SaveChangesAsync(); }
        return NoContent();
    }

    /// <summary>
    /// Returns the authenticated user's profile information.
    /// Requires a valid JWT. Used by the frontend to populate the profile page and navbar.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.Id.ToString(), user.Email, user.FirstName, user.LastName, user.Role, user.PhoneNumber));
    }

    /// <summary>
    /// Updates the authenticated user's profile (name and phone number).
    /// Re-issues a new access token so updated name claims are reflected immediately
    /// without requiring a full logout/login cycle.
    /// Requires a valid JWT.
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest req)
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();
        user.FirstName = req.FirstName;
        user.LastName = req.LastName;
        user.PhoneNumber = req.PhoneNumber;
        await db.SaveChangesAsync();
        // Re-issue token with updated name claims
        var accessToken = jwt.GenerateAccessToken(user);
        return Ok(new { user = new UserDto(user.Id.ToString(), user.Email, user.FirstName, user.LastName, user.Role, user.PhoneNumber), accessToken });
    }

    /// <summary>
    /// Changes the authenticated user's password.
    /// Verifies the current password before applying the change.
    /// Returns 400 if the current password is incorrect.
    /// Requires a valid JWT.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();
        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { error = "Current password is incorrect" });
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Authenticates or registers a user via Google OAuth2.
    /// Verifies the provided Google token against Google's userinfo endpoint.
    /// If the Google account is already linked or the email matches an existing user,
    /// the existing account is used. Otherwise, a new Customer account is auto-created.
    /// Returns the same <see cref="AuthResponse"/> as a standard login.
    /// </summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth(GoogleAuthRequest req)
    {
        // Verify token via Google's userinfo endpoint (works for both access_token and id_token)
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", req.IdToken);
        var res = await http.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
        if (!res.IsSuccessStatusCode)
            return Unauthorized(new { error = "Invalid Google token" });

        var payload = await res.Content.ReadFromJsonAsync<GoogleTokenPayload>();
        if (payload == null || string.IsNullOrEmpty(payload.Sub))
            return Unauthorized(new { error = "Invalid Google token payload" });

        // Find existing user by GoogleId or email
        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Sub)
                ?? await db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email.ToLower());

        if (user == null)
        {
            // Auto-register new Google user
            user = new AppUser
            {
                Email = payload.Email.ToLower(),
                FirstName = payload.GivenName ?? payload.Name?.Split(' ')[0] ?? "User",
                LastName = payload.FamilyName ?? (payload.Name?.Contains(' ') == true ? payload.Name.Split(' ')[1] : ""),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                GoogleId = payload.Sub,
                Role = "Customer"
            };
            db.Users.Add(user);
        }
        else
        {
            user.GoogleId = payload.Sub;
        }

        var accessToken = jwt.GenerateAccessToken(user);
        var refreshToken = jwt.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        return Ok(new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddHours(1).ToString("o"), user.Role, user.Id.ToString()));
    }
}

/// <summary>
/// Internal model representing the relevant fields from Google's OAuth2 userinfo endpoint response.
/// Used only within this file to deserialize the Google API response.
/// </summary>
file sealed class GoogleTokenPayload
{
    [System.Text.Json.Serialization.JsonPropertyName("sub")]
    public string Sub { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("given_name")]
    public string? GivenName { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }
}
