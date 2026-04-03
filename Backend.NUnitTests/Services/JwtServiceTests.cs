using System.IdentityModel.Tokens.Jwt;
using Backend.Models;
using Backend.Services;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Backend.NUnitTests.Services;

[TestFixture]
public class JwtServiceTests
{
    private JwtService _sut = null!;
    private AppUser _user = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]           = "super-secret-test-key-min32chars!!",
                ["Jwt:Issuer"]        = "TestIssuer",
                ["Jwt:Audience"]      = "TestAudience",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

        _sut = new JwtService(config);
        _user = new AppUser
        {
            Id        = Guid.NewGuid(),
            Email     = "test@example.com",
            FirstName = "John",
            LastName  = "Doe",
            Role      = "Customer"
        };
    }

    [Test]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        var token = _sut.GenerateAccessToken(_user);
        Assert.That(token, Is.Not.Empty);
    }

    [Test]
    public void GenerateAccessToken_ContainsCorrectEmail()
    {
        var token = _sut.GenerateAccessToken(_user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var email = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value;
        Assert.That(email, Is.EqualTo(_user.Email));
    }

    [Test]
    public void GenerateAccessToken_ContainsCorrectUserId()
    {
        var token = _sut.GenerateAccessToken(_user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var sub = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        Assert.That(sub, Is.EqualTo(_user.Id.ToString()));
    }

    [Test]
    public void GenerateAccessToken_ContainsCorrectRole()
    {
        var token = _sut.GenerateAccessToken(_user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var role = jwt.Claims.First(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Value;
        Assert.That(role, Is.EqualTo("Customer"));
    }

    [Test]
    [TestCase("Admin")]
    [TestCase("StoreManager")]
    [TestCase("DeliveryDriver")]
    [TestCase("Customer")]
    public void GenerateAccessToken_AnyRole_ClaimIsCorrect(string role)
    {
        _user.Role = role;
        var token = _sut.GenerateAccessToken(_user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roleClaim = jwt.Claims.First(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Value;
        Assert.That(roleClaim, Is.EqualTo(role));
    }

    [Test]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        var t1 = _sut.GenerateRefreshToken();
        var t2 = _sut.GenerateRefreshToken();
        Assert.That(t1, Is.Not.EqualTo(t2));
    }

    [Test]
    public void GenerateRefreshToken_IsValidBase64With64Bytes()
    {
        var token = _sut.GenerateRefreshToken();
        var bytes = Convert.FromBase64String(token);
        Assert.That(bytes.Length, Is.EqualTo(64));
    }
}
