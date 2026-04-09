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

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthDbContext db, JwtService jwt) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID claim not found"));
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

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user != null) { user.RefreshToken = null; await db.SaveChangesAsync(); }
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.Id.ToString(), user.Email, user.FirstName, user.LastName, user.Role, user.PhoneNumber));
    }

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

// Google tokeninfo response shape
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
