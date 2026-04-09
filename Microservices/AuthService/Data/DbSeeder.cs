using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public static class DbSeeder
{
    // Fixed GUIDs so users always have the same ID across restarts
    private static readonly Guid AdminId    = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ManagerId  = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DriverId   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CustomerId = Guid.Parse("44444444-4444-4444-4444-444444444444");

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
