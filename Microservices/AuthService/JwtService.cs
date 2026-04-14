using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services;

/// <summary>
/// Responsible for generating JWT access tokens and opaque refresh tokens.
/// Token configuration (key, issuer, audience, expiry) is read from <c>appsettings.json</c> / environment variables.
/// </summary>
public class JwtService(IConfiguration config)
{
    /// <summary>
    /// Generates a signed JWT access token for the given user.
    /// Embeds the user's ID, email, name, and role as standard claims so downstream
    /// microservices can authorize requests without calling back to AuthService.
    /// </summary>
    /// <param name="user">The authenticated user whose claims will be embedded in the token.</param>
    /// <returns>A compact, URL-safe JWT string signed with HMAC-SHA256.</returns>
    public string GenerateAccessToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:ExpiryMinutes"] ?? "60"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically random opaque refresh token.
    /// The token is 64 bytes of random data encoded as Base64, making it
    /// effectively unguessable. It is stored hashed in the database and
    /// rotated on every use to prevent replay attacks.
    /// </summary>
    /// <returns>A Base64-encoded 64-byte random string.</returns>
    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
