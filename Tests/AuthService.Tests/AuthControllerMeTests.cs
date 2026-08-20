using System.Security.Claims;
using AuthService.Controllers;
using AuthService.Core.DTOs;
using AuthService.Core.Interfaces;
using AuthService.Core.Models;
using AuthService.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;

namespace AuthService.Tests;

/// <summary>
/// Unit tests for GET /api/v1/auth/me endpoint.
/// This endpoint returns the logged in user's profile using their ID from the JWT.
/// </summary>
public class AuthControllerMeTests
{
    /// <summary>Test 1: user exists in DB, should return 200 with correct data.</summary>
    [Fact]
    public async Task Me_ShouldReturnUser_WhenUserExists()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase("test_db_1")
            .Options;

        var db = new AuthDbContext(options);

        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser
        {
            Id = userId,
            Email = "test@gmail.com",
            FirstName = "Ali",
            LastName = "Khan",
            Role = "Customer",
            PhoneNumber = "9999999999",
            PasswordHash = "doesntmatter"
        });
        await db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
        };
        var httpCtx = new DefaultHttpContext();
        httpCtx.User = new System.Security.Claims.ClaimsPrincipal(
            new ClaimsIdentity(claims, "Test"));

        var controller = new AuthController(db, new Mock<IJwtService>().Object);
        controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await controller.Me();

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<UserDto>(ok.Value);

        Assert.Equal("test@gmail.com", dto.Email);
        Assert.Equal("Ali", dto.FirstName);
        Assert.Equal("Customer", dto.Role);
    }

    /// <summary>Test 2: user ID in token doesn't exist in DB, should return 404.</summary>
    [Fact]
    public async Task Me_ShouldReturn404_WhenUserNotFound()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase("test_db_2")
            .Options;

        var db = new AuthDbContext(options);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())
        };
        var httpCtx = new DefaultHttpContext();
        httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var controller = new AuthController(db, new Mock<IJwtService>().Object);
        controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await controller.Me();

        Assert.IsType<NotFoundResult>(result);
    }
}
