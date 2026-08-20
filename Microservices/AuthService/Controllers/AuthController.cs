using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Core.DTOs;
using AuthService.Core.Interfaces;
using AuthService.Core.Models;
using AuthService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthDbContext db, IJwtService jwt) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID not found"));

    // POST /api/v1/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email.ToLower()))
            return Conflict(new { error = "Email already registered" });

        var user = new AppUser
        {
            Email        = req.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FirstName    = req.FirstName,
            LastName     = req.LastName,
            PhoneNumber  = req.PhoneNumber
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(new { userId = user.Id, email = user.Email, role = user.Role });
    }

    // POST /api/v1/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid email or password" });

        var accessToken  = jwt.GenerateAccessToken(user);
        var refreshToken = jwt.GenerateRefreshToken();

        user.RefreshToken       = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        return Ok(new AuthResponse(
            accessToken, refreshToken,
            DateTime.UtcNow.AddHours(1).ToString("o"),
            user.Role, user.Id.ToString()));
    }

    // GET /api/v1/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();

        return Ok(new UserDto(
            user.Id.ToString(), user.Email,
            user.FirstName, user.LastName,
            user.Role, user.PhoneNumber));
    }
}
