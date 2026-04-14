using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

/// <summary>
/// Seeds the AuthService database with a default set of users on startup.
/// Uses fixed GUIDs so seeded users always have the same IDs across container restarts,
/// which is important for cross-service references (e.g., order history, notifications).
/// Safe to run on every startup — uses upsert logic to avoid duplicates.
/// </summary>
public static class DbSeeder
{
    // Fixed GUIDs so users always have the same ID across restarts
    private static readonly Guid AdminId    = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ManagerId  = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DriverId   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CustomerId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    /// <summary>
    /// Ensures the database schema exists and upserts the default seed users.
    /// Creates one user per role: Admin, StoreManager, DeliveryDriver, and Customer.
    /// Passwords are BCrypt-hashed before storage.
    /// </summary>
    /// <param name="db">The AuthDbContext instance to seed.</param>
    public static async Task SeedAsync(AuthDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        // Upsert each user by email — safe to run on every startup
        await UpsertUser(db, AdminId,    "aryandalal081@gmail.com", "Admin@123",    "Aryan",    "Admin",    "Admin");
        await UpsertUser(db, ManagerId,  "manager@grocery.com",     "Manager@123",  "Store",    "Manager",  "StoreManager");
        await UpsertUser(db, DriverId,   "driver@grocery.com",      "Driver@123",   "Delivery", "Driver",   "DeliveryDriver");
        await UpsertUser(db, CustomerId, "kajaldalal081@gmail.com",  "Customer@123", "Kajal",    "Customer", "Customer");

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Inserts a new user if no user with the given email exists.
    /// Skips the insert silently if the email is already registered, preventing duplicate seed data.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="id">Fixed GUID to assign as the user's primary key.</param>
    /// <param name="email">Email address (used as the unique lookup key).</param>
    /// <param name="password">Plain-text password that will be BCrypt-hashed before storage.</param>
    /// <param name="firstName">User's given name.</param>
    /// <param name="lastName">User's family name.</param>
    /// <param name="role">Role to assign (e.g., <c>Admin</c>, <c>Customer</c>).</param>
    private static async Task UpsertUser(AuthDbContext db, Guid id, string email, string password,
        string firstName, string lastName, string role)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing == null)
        {
            db.Users.Add(new AppUser
            {
                Id = id,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                IsActive = true
            });
        }
    }
}
