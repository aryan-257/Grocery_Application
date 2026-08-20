using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Controllers;
using ProductService.Core.Models;
using ProductService.Infrastructure.Data;

namespace ProductService.Tests;

/// <summary>
/// Unit tests for GET /api/v1/products endpoint.
/// No auth required, just reads from DB with optional filters.
/// </summary>
public class ProductsControllerGetTests
{
    /// <summary>Test 1: products exist in DB, should return 200 OK.</summary>
    [Fact]
    public async Task GetProducts_ShouldReturnProducts_WhenProductsExist()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase("products_test_db_1")
            .Options;

        var db = new ProductDbContext(options);

        var category = new Category { Id = Guid.NewGuid(), Name = "Vegetables" };
        db.Categories.Add(category);

        db.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Name = "Tomato",
            Description = "Fresh tomatoes",
            Price = 30,
            Sku = "VEG001",
            ImageUrl = "tomato.jpg",
            CategoryId = category.Id,
            Category = category,
            StockQuantity = 100,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var controller = new ProductsController(db);
        var result = await controller.GetProducts(null, null, 1, 20);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    /// <summary>Test 2: inactive products should not show up in the list.</summary>
    [Fact]
    public async Task GetProducts_ShouldNotReturn_InactiveProducts()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase("products_test_db_2")
            .Options;

        var db = new ProductDbContext(options);

        var category = new Category { Id = Guid.NewGuid(), Name = "Fruits" };
        db.Categories.Add(category);

        db.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Name = "Old Mango",
            Description = "Expired listing",
            Price = 50,
            Sku = "FRT001",
            ImageUrl = "mango.jpg",
            CategoryId = category.Id,
            Category = category,
            StockQuantity = 0,
            IsActive = false
        });
        await db.SaveChangesAsync();

        var controller = new ProductsController(db);
        var result = await controller.GetProducts(null, null, 1, 20);

        var ok = Assert.IsType<OkObjectResult>(result);
        var total = ok.Value!.GetType().GetProperty("total")?.GetValue(ok.Value);
        Assert.Equal(0, total);
    }
}
