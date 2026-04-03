using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Backend.NUnitTests.Controllers;

[TestFixture]
public class ProductsControllerTests
{
    private AppDbContext _db = null!;
    private Guid _catId;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);

        var cat = new Category { Id = Guid.NewGuid(), Name = "Pantry" };
        _catId = cat.Id;
        _db.Categories.Add(cat);
        _db.Products.AddRange(
            new Product { Name = "Atta", Sku = "P001", Price = 280, CategoryId = _catId, StockQuantity = 50, IsActive = true },
            new Product { Name = "Rice", Sku = "P002", Price = 120, CategoryId = _catId, StockQuantity = 5,  IsActive = true },
            new Product { Name = "Salt", Sku = "P003", Price = 20,  CategoryId = _catId, StockQuantity = 0,  IsActive = false }
        );
        _db.SaveChanges();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetProducts_ReturnsOnlyActiveProducts()
    {
        var ctrl = new ProductsController(_db);
        var result = await ctrl.GetProducts(null, null, null, null, null) as OkObjectResult;
        var paged = result!.Value as PaginatedResult<ProductDto>;

        Assert.That(paged!.Total, Is.EqualTo(2)); // Salt is inactive
    }

    [Test]
    public async Task GetProducts_SearchByName_FiltersCorrectly()
    {
        var ctrl = new ProductsController(_db);
        var result = await ctrl.GetProducts("Atta", null, null, null, null) as OkObjectResult;
        var paged = result!.Value as PaginatedResult<ProductDto>;

        Assert.That(paged!.Total, Is.EqualTo(1));
        Assert.That(paged.Items.First().Name, Is.EqualTo("Atta"));
    }

    [Test]
    public async Task GetProducts_SearchBySku_FiltersCorrectly()
    {
        var ctrl = new ProductsController(_db);
        var result = await ctrl.GetProducts("P002", null, null, null, null) as OkObjectResult;
        var paged = result!.Value as PaginatedResult<ProductDto>;

        Assert.That(paged!.Total, Is.EqualTo(1));
        Assert.That(paged.Items.First().Sku, Is.EqualTo("P002"));
    }

    [Test]
    public async Task GetProducts_FilterByCategory_ReturnsOnlyThatCategory()
    {
        var ctrl = new ProductsController(_db);
        var result = await ctrl.GetProducts(null, _catId, null, null, null) as OkObjectResult;
        var paged = result!.Value as PaginatedResult<ProductDto>;

        Assert.That(paged!.Items.All(p => p.CategoryName == "Pantry"), Is.True);
    }

    [Test]
    public async Task GetProduct_ValidId_ReturnsProduct()
    {
        var id = _db.Products.First(p => p.Name == "Atta").Id;
        var ctrl = new ProductsController(_db);

        var result = await ctrl.GetProduct(id) as OkObjectResult;
        var dto = result!.Value as ProductDto;

        Assert.That(dto!.Name, Is.EqualTo("Atta"));
        Assert.That(dto.Price, Is.EqualTo(280m));
    }

    [Test]
    public async Task GetProduct_InvalidId_ReturnsNotFound()
    {
        var ctrl = new ProductsController(_db);
        var result = await ctrl.GetProduct(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task LowStock_ReturnsProductsWithLessThan10Units()
    {
        var ctrl = new ProductsController(_db);
        var result = await ctrl.LowStock() as OkObjectResult;
        var items = result!.Value as IEnumerable<ProductDto>;

        Assert.That(items!.Count(), Is.EqualTo(2)); // Rice (5) and Salt (0)
        Assert.That(items!.Any(p => p.Name == "Rice"), Is.True);
    }

    [Test]
    public async Task UpdateStock_ChangesQuantity()
    {
        var id = _db.Products.First(p => p.Name == "Atta").Id;
        var ctrl = new ProductsController(_db);

        await ctrl.UpdateStock(id, new UpdateStockRequest(100));

        var updated = await _db.Products.FindAsync(id);
        Assert.That(updated!.StockQuantity, Is.EqualTo(100));
    }

    [Test]
    public async Task UpdateStock_InvalidId_ReturnsNotFound()
    {
        var ctrl = new ProductsController(_db);
        var result = await ctrl.UpdateStock(Guid.NewGuid(), new UpdateStockRequest(10));

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    [TestCase(-10)]
    [TestCase(150)]
    public async Task UpdateDiscount_InvalidPercent_ReturnsBadRequest(decimal percent)
    {
        var id = _db.Products.First().Id;
        var ctrl = new ProductsController(_db);

        var result = await ctrl.UpdateDiscount(id, new UpdateDiscountRequest(percent));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    [TestCase(0)]
    [TestCase(10)]
    [TestCase(50)]
    [TestCase(100)]
    public async Task UpdateDiscount_ValidPercent_UpdatesProduct(decimal percent)
    {
        var id = _db.Products.First(p => p.Name == "Atta").Id;
        var ctrl = new ProductsController(_db);

        await ctrl.UpdateDiscount(id, new UpdateDiscountRequest(percent));

        var updated = await _db.Products.FindAsync(id);
        Assert.That(updated!.DiscountPercent, Is.EqualTo(percent));
    }

    [Test]
    public async Task OnSale_ReturnsOnlyDiscountedProducts()
    {
        var id = _db.Products.First(p => p.Name == "Atta").Id;
        await new ProductsController(_db).UpdateDiscount(id, new UpdateDiscountRequest(10));

        var result = await new ProductsController(_db).OnSale() as OkObjectResult;
        var items = result!.Value as IEnumerable<ProductDto>;

        Assert.That(items!.Count(), Is.EqualTo(1));
        Assert.That(items!.First().DiscountPercent, Is.GreaterThan(0));
    }
}
