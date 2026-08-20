using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Core.Models;
using ProductService.Infrastructure.Data;

namespace ProductService.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController(ProductDbContext db) : ControllerBase
{
    // GET /api/v1/categories
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await db.Categories.ToListAsync();
        return Ok(categories.Select(c => new { id = c.Id, name = c.Name }));
    }

    // POST /api/v1/categories
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest req)
    {
        var category = new Category
        {
            Name = req.Name,
            Description = req.Description
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return Ok(new { id = category.Id, name = category.Name });
    }
}

public record CreateCategoryRequest(string Name, string? Description);
