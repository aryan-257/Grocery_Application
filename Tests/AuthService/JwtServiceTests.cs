using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using System.Text;

namespace FreshMart.Tests.AuthService;

// Inline AppUser to avoid project reference
public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public string PasswordHash { get; set; } = string.Empty;
}

// Inline JwtService to avoid project reference
public class JwtService(IConfiguration config)
{
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

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}

[TestFixture]
public class JwtServiceTests
{
    private JwtService _jwtService = null!;
    private IConfiguration _config = null!;
    private const string TestKey = "super-secret-jwt-key-change-in-production-min32chars!";

    [SetUp]
    public void SetUp()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = TestKey,
            ["Jwt:Issuer"] = "GroceryApp",
            ["Jwt:Audience"] = "GroceryApp",
            ["Jwt:ExpiryMinutes"] = "60"
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        _jwtService = new JwtService(_config);
    }

    [Test]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        var user = new AppUser { Email = "test@test.com", FirstName = "Test", LastName = "User", Role = "Customer" };
        var token = _jwtService.GenerateAccessToken(user);
        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GenerateAccessToken_ContainsCorrectEmail()
    {
        var user = new AppUser { Email = "kajal@test.com", FirstName = "Kajal", LastName = "Dalal", Role = "Customer" };
        var token = _jwtService.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.That(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value, Is.EqualTo("kajal@test.com"));
    }

    [Test]
    public void GenerateAccessToken_ContainsCorrectRole()
    {
        var user = new AppUser { Email = "admin@test.com", FirstName = "Admin", LastName = "User", Role = "Admin" };
        var token = _jwtService.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.That(jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value, Is.EqualTo("Admin"));
    }

    [Test]
    public void GenerateAccessToken_ContainsCorrectUserId()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser { Id = userId, Email = "test@test.com", FirstName = "Test", LastName = "User", Role = "Customer" };
        var token = _jwtService.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.That(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value, Is.EqualTo(userId.ToString()));
    }

    [Test]
    public void GenerateAccessToken_IsValidJwt()
    {
        var user = new AppUser { Email = "test@test.com", FirstName = "Test", LastName = "User", Role = "Customer" };
        var token = _jwtService.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "GroceryApp",
            ValidAudience = "GroceryApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey))
        };

        Assert.DoesNotThrow(() => handler.ValidateToken(token, validationParams, out _));
    }

    [Test]
    public void GenerateAccessToken_ExpiresInCorrectTime()
    {
        var user = new AppUser { Email = "test@test.com", FirstName = "Test", LastName = "User", Role = "Customer" };
        var before = DateTime.UtcNow;
        var token = _jwtService.GenerateAccessToken(user);
        var after = DateTime.UtcNow;

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.That(jwt.ValidTo, Is.GreaterThan(before.AddMinutes(59)));
        Assert.That(jwt.ValidTo, Is.LessThan(after.AddMinutes(61)));
    }

    [Test]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var token = _jwtService.GenerateRefreshToken();
        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GenerateRefreshToken_ReturnsDifferentTokensEachTime()
    {
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();
        Assert.That(token1, Is.Not.EqualTo(token2));
    }

    [Test]
    public void GenerateRefreshToken_IsBase64String()
    {
        var token = _jwtService.GenerateRefreshToken();
        Assert.DoesNotThrow(() => Convert.FromBase64String(token));
    }
}
