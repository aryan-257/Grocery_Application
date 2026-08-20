using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Core.Interfaces;
using AuthService.Core.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// Generates JWT access tokens and opaque refresh tokens.
/// Token config (key, issuer, audience, expiry) is read from appsettings / environment variables.
/// </summary>
public class JwtService(IConfiguration config) : IJwtService
{
    /// <inheritdoc/>
    public string GenerateAccessToken(AppUser user)
    {
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(
            double.Parse(config["Jwt:ExpiryMinutes"] ?? "60"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,        user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email,      user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName,  user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(ClaimTypes.Role,                    user.Role),
            // jti makes every token unique — useful for revocation checks later
            new Claim(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             config["Jwt:Issuer"],
            audience:           config["Jwt:Audience"],
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc/>
    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
