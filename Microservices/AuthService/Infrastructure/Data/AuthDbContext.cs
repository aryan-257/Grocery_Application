using AuthService.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Data;

/// <summary>
/// EF Core database context for AuthService.
/// Manages user accounts and handles persistence to the underlying SQLite database.
/// </summary>
public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    /// <summary>All registered user accounts in the system.</summary>
    public DbSet<AppUser> Users => Set<AppUser>();
}
