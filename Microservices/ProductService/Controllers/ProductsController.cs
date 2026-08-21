using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Core.DTOs;
using ProductService.Core.Models;
using ProductService.Infrastructure.Data;

namespace ProductService.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController(ProductDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.Contains(search) ||
                p.Description.Contains(search) ||
                p.Category.Name.Contains(search));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var idStr = id.ToString().ToLower();
        var product = await db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id.ToString().ToLower() == idStr);

        if (product == null) return NotFound();
        return Ok(MapToDto(product));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> CreateProduct(CreateProductRequest req)
    {
        var catIdStr = req.CategoryId.ToString().ToLower();
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id.ToString().ToLower() == catIdStr);
        if (category == null)
            return BadRequest(new { error = "Category not found" });

        var product = new Product
        {
            Name = req.Name,
            Description = req.Description,
            Price = req.Price,
            Sku = req.Sku,
            ImageUrl = req.ImageUrl,
            CategoryId = category.Id,
            StockQuantity = req.StockQuantity,
            Brand = req.Brand,
            Unit = req.Unit
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        product.Category = category;
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, MapToDto(product));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductRequest req)
    {
        var idStr = id.ToString().ToLower();
        var product = await db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id.ToString().ToLower() == idStr);

        if (product == null) return NotFound();

        var catIdStr = req.CategoryId.ToString().ToLower();
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id.ToString().ToLower() == catIdStr);
        if (category == null)
            return BadRequest(new { error = "Category not found" });

        product.Name = req.Name;
        product.Description = req.Description;
        product.Price = req.Price;
        product.Sku = req.Sku;
        product.ImageUrl = req.ImageUrl;
        product.CategoryId = category.Id;
        product.Category = category;
        product.StockQuantity = req.StockQuantity;
        product.Brand = req.Brand;
        product.Unit = req.Unit;
        product.DiscountPercent = req.DiscountPercent;
        product.IsActive = req.IsActive;

        await db.SaveChangesAsync();
        return Ok(MapToDto(product));
    }

    // soft delete - just mark as inactive
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var idStr = id.ToString().ToLower();
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id.ToString().ToLower() == idStr);
        if (product == null) return NotFound();

        product.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static ProductDto MapToDto(Product p)
    {
        var finalPrice = p.DiscountPercent > 0
            ? Math.Round(p.Price * (1 - p.DiscountPercent / 100m), 2)
            : p.Price;

        return new ProductDto(
            p.Id.ToString(), p.Name, p.Description, p.Price, p.Sku,
            p.ImageUrl, p.Category?.Name ?? string.Empty,
            p.StockQuantity, p.IsActive, p.AverageRating,
            p.Brand, p.Unit, p.DiscountPercent, finalPrice);
    }
}
