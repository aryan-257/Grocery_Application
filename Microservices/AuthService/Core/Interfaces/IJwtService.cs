using AuthService.Core.Models;

namespace AuthService.Core.Interfaces;

/// <summary>
/// Defines the contract for generating JWT access tokens and opaque refresh tokens.
/// Keeps the controller and seeder decoupled from the concrete token generation logic.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a signed JWT access token for the given user.
    /// Embeds the user's ID, email, name, and role as standard claims.
    /// </summary>
    /// <param name="user">The authenticated user whose claims will be embedded.</param>
    /// <returns>A compact, URL-safe JWT string signed with HMAC-SHA256.</returns>
    string GenerateAccessToken(AppUser user);

    /// <summary>
    /// Generates a cryptographically random opaque refresh token.
    /// 64 bytes of random data, Base64-encoded — effectively unguessable.
    /// </summary>
    /// <returns>A Base64-encoded 64-byte random string.</returns>
    string GenerateRefreshToken();
}
