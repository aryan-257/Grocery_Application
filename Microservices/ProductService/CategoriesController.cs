using ProductService.Data;
using ProductService.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProductService.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController(ProductDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cats = await db.Categories
            .Select(c => new CategoryDto(c.Id.ToString(), c.Name, c.Description, c.ImageUrl, c.ParentCategoryId.HasValue ? c.ParentCategoryId.ToString() : null))
            .ToListAsync();
        return Ok(cats);
    }
}
