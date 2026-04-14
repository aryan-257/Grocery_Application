using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

/// <summary>
/// Entity Framework Core database context for the AuthService.
/// Manages the <see cref="AppUser"/> entity and its persistence to the underlying database.
/// </summary>
public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The set of all registered users in the system.
    /// Used for authentication, token management, and user administration.
    /// </summary>
    public DbSet<AppUser> Users => Set<AppUser>();
}
