using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProductService.Controllers;

/// <summary>
/// Manages the FreshMart product catalogue.
/// Public endpoints allow browsing, searching, and filtering products.
/// Write endpoints (create, update, delete, stock, discount) are restricted to Admin and StoreManager roles.
/// </summary>
[ApiController]
[Route("api/v1/products")]
public class ProductsController(ProductDbContext db) : ControllerBase
{
    /// <summary>
    /// Returns a paginated list of active products with optional filtering and sorting.
    /// Supports full-text search across name, description, brand, SKU, and category name.
    /// Accessible by: all users (anonymous).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? query, [FromQuery] Guid? categoryId,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
        [FromQuery] string? sortBy, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = db.Products.Include(p => p.Category).Where(p => p.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(p =>
                p.Name.Contains(query) ||
                p.Description.Contains(query) ||
                p.Brand!.Contains(query) ||
                p.Sku.Contains(query) ||
                p.Category.Name.Contains(query));
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);
        if (minPrice.HasValue) q = q.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue) q = q.Where(p => p.Price <= maxPrice);
        q = sortBy switch
        {
            "price_asc" => q.OrderBy(p => p.Price),
            "price_desc" => q.OrderByDescending(p => p.Price),
            "rating" => q.OrderByDescending(p => p.AverageRating),
            _ => q.OrderBy(p => p.Name)
        };
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => ToDto(p))
            .ToListAsync();
        return Ok(new PaginatedResult<ProductDto>(items, total, page, pageSize));
    }

    /// <summary>
    /// Returns up to 6 lightweight product suggestions for the search autocomplete dropdown.
    /// Matches against product name, brand, and category name.
    /// Returns an empty array if the query is shorter than 2 characters.
    /// Accessible by: all users (anonymous).
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> Suggestions([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return Ok(Array.Empty<object>());
        var results = await db.Products
            .Where(p => p.IsActive && (p.Name.Contains(q) || p.Brand!.Contains(q) || p.Category.Name.Contains(q)))
            .OrderBy(p => p.Name)
            .Take(6)
            .Select(p => new { p.Id, p.Name, p.ImageUrl, CategoryName = p.Category.Name, p.Price })
            .ToListAsync();
        return Ok(results);
    }

    /// <summary>
    /// Returns the full details of a single product by its ID.
    /// Returns 404 if the product does not exist or is inactive.
    /// Accessible by: all users (anonymous).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var p = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (p == null) return NotFound();
        return Ok(ToDto(p));
    }

    /// <summary>
    /// Returns all products with fewer than 10 units in stock.
    /// Used by the Admin/StoreManager dashboard to identify items that need restocking.
    /// Accessible by: Admin, StoreManager.
    /// </summary>
    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> LowStock()
    {
        var items = await db.Products.Include(p => p.Category).Where(p => p.StockQuantity < 10)
            .Select(p => ToDto(p))
            .ToListAsync();
        return Ok(items);
    }

    /// <summary>
    /// Creates a new product in the catalogue.
    /// Validates that the specified category exists before creating the product.
    /// Returns 201 Created with the new product's details.
    /// Accessible by: Admin, StoreManager.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> Create(CreateProductRequest req)
    {
        var category = await db.Categories.FindAsync(req.CategoryId);
        if (category == null) return BadRequest(new { error = "Category not found" });
        var product = new Product { Name = req.Name, Description = req.Description, Price = req.Price, Sku = req.Sku, ImageUrl = req.ImageUrl, CategoryId = req.CategoryId, StockQuantity = req.StockQuantity, Brand = req.Brand, Unit = req.Unit };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ToDto(product, category.Name));
    }

    /// <summary>
    /// Fully updates an existing product's details.
    /// Validates that the new category exists. Replaces all editable fields.
    /// Accessible by: Admin, StoreManager.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest req)
    {
        var product = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        var category = await db.Categories.FindAsync(req.CategoryId);
        if (category == null) return BadRequest(new { error = "Category not found" });
        product.Name = req.Name;
        product.Description = req.Description;
        product.Price = req.Price;
        product.Sku = req.Sku;
        product.ImageUrl = req.ImageUrl;
        product.CategoryId = req.CategoryId;
        product.StockQuantity = req.StockQuantity;
        product.Brand = req.Brand;
        product.Unit = req.Unit;
        product.DiscountPercent = req.DiscountPercent;
        product.IsActive = req.IsActive;
        await db.SaveChangesAsync();
        return Ok(ToDto(product, category.Name));
    }

    /// <summary>
    /// Soft-deletes a product by setting <c>IsActive = false</c>.
    /// The product is hidden from all public listings but its data is preserved.
    /// Returns 204 No Content on success.
    /// Accessible by: Admin only.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return NotFound();
        product.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Updates the stock quantity for a product to the specified absolute value.
    /// Used by StoreManagers after restocking. Returns 204 No Content on success.
    /// Accessible by: Admin, StoreManager.
    /// </summary>
    [HttpPatch("{id}/stock")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> UpdateStock(Guid id, UpdateStockRequest req)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return NotFound();
        product.StockQuantity = req.Quantity;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Sets the discount percentage for a product (0–100).
    /// A discount of 0 removes any active promotion.
    /// Returns 400 if the value is outside the valid range.
    /// Accessible by: Admin, StoreManager.
    /// </summary>
    [HttpPatch("{id}/discount")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> UpdateDiscount(Guid id, UpdateDiscountRequest req)
    {
        if (req.DiscountPercent < 0 || req.DiscountPercent > 100)
            return BadRequest(new { error = "Discount must be between 0 and 100" });
        var product = await db.Products.FindAsync(id);
        if (product == null) return NotFound();
        product.DiscountPercent = req.DiscountPercent;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Returns all active products that currently have a discount applied, ordered by highest discount first.
    /// Used to populate the "Offers" / "On Sale" page in the frontend.
    /// Accessible by: all users (anonymous).
    /// </summary>
    [HttpGet("on-sale")]
    public async Task<IActionResult> OnSale()
    {
        var items = await db.Products.Include(p => p.Category)
            .Where(p => p.IsActive && p.DiscountPercent > 0)
            .OrderByDescending(p => p.DiscountPercent)
            .Select(p => ToDto(p))
            .ToListAsync();
        return Ok(items);
    }

    /// <summary>
    /// Maps a <see cref="Product"/> entity to a <see cref="ProductDto"/>.
    /// Computes the discounted price when a discount is active.
    /// </summary>
    private static ProductDto ToDto(Product p, string? categoryName = null)
    {
        var cat = categoryName ?? p.Category?.Name ?? "";
        var discounted = p.DiscountPercent > 0
            ? Math.Round(p.Price * (1 - p.DiscountPercent / 100m), 2)
            : p.Price;
        return new ProductDto(p.Id.ToString(), p.Name, p.Description, p.Price, p.Sku,
            p.ImageUrl, cat, p.StockQuantity, p.IsActive, p.AverageRating,
            p.Brand, p.Unit, p.DiscountPercent, discounted);
    }
}
