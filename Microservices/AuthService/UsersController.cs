using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

/// <summary>
/// Admin-only controller for managing all user accounts in the FreshMart platform.
/// Provides CRUD operations, role management, and account activation toggling.
/// All endpoints require the <c>Admin</c> role.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public class UsersController(AuthDbContext db) : ControllerBase
{
    /// <summary>
    /// Returns a filtered, sorted list of all users.
    /// Supports optional filtering by role, active status, and a text search across email and name fields.
    /// Results are ordered by creation date descending (newest first).
    /// Accessible by: Admin.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? role, [FromQuery] string? search, [FromQuery] bool? isActive)
    {
        var q = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(role)) q = q.Where(u => u.Role == role);
        if (isActive.HasValue) q = q.Where(u => u.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(u => u.Email.Contains(s) || u.FirstName.Contains(s) || u.LastName.Contains(s));
        }
        var users = await q.OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserAdminDto(u.Id.ToString(), u.Email, u.FirstName, u.LastName, u.Role, u.PhoneNumber, u.IsActive, u.CreatedAt))
            .ToListAsync();
        return Ok(users);
    }

    /// <summary>
    /// Returns aggregate statistics about the user base.
    /// Includes total user count, active/inactive breakdown, and a per-role count.
    /// Useful for the Admin dashboard overview.
    /// Accessible by: Admin.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var total = await db.Users.CountAsync();
        var byRole = await db.Users.GroupBy(u => u.Role)
            .Select(g => new { role = g.Key, count = g.Count() })
            .ToListAsync();
        var active = await db.Users.CountAsync(u => u.IsActive);
        return Ok(new { total, active, inactive = total - active, byRole });
    }

    /// <summary>
    /// Returns a single user's full admin-level profile by their unique ID.
    /// Returns 404 if no user with the given ID exists.
    /// Accessible by: Admin.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        return Ok(new UserAdminDto(u.Id.ToString(), u.Email, u.FirstName, u.LastName, u.Role, u.PhoneNumber, u.IsActive, u.CreatedAt));
    }

    /// <summary>
    /// Updates a user's profile fields (email, name, phone).
    /// Only non-null/non-empty fields in the request are applied.
    /// Validates email uniqueness before changing it.
    /// Returns 409 Conflict if the new email is already taken by another user.
    /// Accessible by: Admin.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest req)
    {
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Email) && req.Email != u.Email)
        {
            if (await db.Users.AnyAsync(x => x.Email == req.Email.ToLower() && x.Id != id))
                return Conflict(new { error = "Email already in use" });
            u.Email = req.Email.ToLower();
        }
        if (!string.IsNullOrWhiteSpace(req.FirstName)) u.FirstName = req.FirstName;
        if (!string.IsNullOrWhiteSpace(req.LastName)) u.LastName = req.LastName;
        u.PhoneNumber = req.PhoneNumber;
        await db.SaveChangesAsync();
        return Ok(new UserAdminDto(u.Id.ToString(), u.Email, u.FirstName, u.LastName, u.Role, u.PhoneNumber, u.IsActive, u.CreatedAt));
    }

    /// <summary>
    /// Changes a user's role to one of the four valid platform roles.
    /// Validates the role value before applying. Returns 400 for unrecognized roles.
    /// This affects the user's permissions across all microservices on their next login.
    /// Accessible by: Admin.
    /// </summary>
    [HttpPatch("{id}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeRoleRequest req)
    {
        var validRoles = new[] { "Admin", "StoreManager", "DeliveryDriver", "Customer" };
        if (!validRoles.Contains(req.Role)) return BadRequest(new { error = "Invalid role" });
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.Role = req.Role;
        await db.SaveChangesAsync();
        return Ok(new { id = u.Id, role = u.Role });
    }

    /// <summary>
    /// Toggles a user's active status between active and inactive.
    /// Inactive users are blocked from logging in.
    /// Returns the updated ID and active status.
    /// Accessible by: Admin.
    /// </summary>
    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.IsActive = !u.IsActive;
        await db.SaveChangesAsync();
        return Ok(new { id = u.Id, isActive = u.IsActive });
    }

    /// <summary>
    /// Permanently deletes a user account from the database.
    /// This action is irreversible. Returns 204 No Content on success.
    /// Accessible by: Admin.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        db.Users.Remove(u);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
