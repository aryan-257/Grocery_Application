using ProductService.Data;
using ProductService.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProductService.Controllers;

/// <summary>
/// Provides read access to the product category hierarchy.
/// Categories are used for navigation, filtering, and product organisation in the frontend.
/// All endpoints are publicly accessible (no authentication required).
/// </summary>
[ApiController]
[Route("api/v1/categories")]
public class CategoriesController(ProductDbContext db) : ControllerBase
{
    /// <summary>
    /// Returns all product categories, including optional parent category references.
    /// Used by the frontend to build the navigation menu and category filter panel.
    /// Accessible by: all users (anonymous).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cats = await db.Categories
            .Select(c => new CategoryDto(c.Id.ToString(), c.Name, c.Description, c.ImageUrl, c.ParentCategoryId.HasValue ? c.ParentCategoryId.ToString() : null))
            .ToListAsync();
        return Ok(cats);
    }
}
