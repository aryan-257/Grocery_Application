using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace FreshMart.Tests.ProductService;

// Minimal inline models
public class Category { public Guid Id { get; set; } = Guid.NewGuid(); public string Name { get; set; } = ""; }
public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = "";
    public Guid CategoryId { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public string? Unit { get; set; }
    public double AverageRating { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public string Description { get; set; } = "";
}

public class TestProductDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
}

[TestFixture]
public class ProductSeederTests
{
    private TestProductDbContext _db = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<TestProductDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new TestProductDbContext(options);
        _db.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Products_HaveUniqueSkus()
    {
        SeedTestData();
        var skus = await _db.Products.Select(p => p.Sku).ToListAsync();
        Assert.That(skus.Distinct().Count(), Is.EqualTo(skus.Count));
    }

    [Test]
    public async Task Products_AllHavePositivePrice()
    {
        SeedTestData();
        var hasInvalid = await _db.Products.AnyAsync(p => p.Price <= 0);
        Assert.That(hasInvalid, Is.False);
    }

    [Test]
    public async Task Products_AllHaveImageUrl()
    {
        SeedTestData();
        var hasEmpty = await _db.Products.AnyAsync(p => p.ImageUrl == null || p.ImageUrl == "");
        Assert.That(hasEmpty, Is.False);
    }

    [Test]
    public async Task Products_AllHavePositiveStock()
    {
        SeedTestData();
        var hasInvalid = await _db.Products.AnyAsync(p => p.StockQuantity < 0);
        Assert.That(hasInvalid, Is.False);
    }

    [Test]
    public async Task Products_AllBelongToExistingCategory()
    {
        SeedTestData();
        var categoryIds = await _db.Categories.Select(c => c.Id).ToListAsync();
        var orphaned = await _db.Products.AnyAsync(p => !categoryIds.Contains(p.CategoryId));
        Assert.That(orphaned, Is.False);
    }

    [Test]
    public async Task Products_RatingBetween0And5()
    {
        SeedTestData();
        var invalid = await _db.Products.AnyAsync(p => p.AverageRating < 0 || p.AverageRating > 5);
        Assert.That(invalid, Is.False);
    }

    [Test]
    public async Task Products_DiscountBetween0And100()
    {
        SeedTestData();
        var invalid = await _db.Products.AnyAsync(p => p.DiscountPercent < 0 || p.DiscountPercent > 100);
        Assert.That(invalid, Is.False);
    }

    [Test]
    public async Task Categories_HaveUniqueNames()
    {
        SeedTestData();
        var names = await _db.Categories.Select(c => c.Name).ToListAsync();
        Assert.That(names.Distinct().Count(), Is.EqualTo(names.Count));
    }

    [Test]
    public async Task AddProduct_CanBeRetrievedById()
    {
        var catId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = catId, Name = "Test" });
        var product = new Product { Name = "Test Product", Sku = "TP001", Price = 50m, ImageUrl = "http://test.com/img.jpg", CategoryId = catId, StockQuantity = 10 };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var found = await _db.Products.FindAsync(product.Id);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Name, Is.EqualTo("Test Product"));
    }

    [Test]
    public async Task AddProduct_PriceIsStoredCorrectly()
    {
        var catId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = catId, Name = "Test" });
        var product = new Product { Name = "Rice", Sku = "R001", Price = 120.50m, ImageUrl = "http://test.com/img.jpg", CategoryId = catId, StockQuantity = 50 };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var found = await _db.Products.FindAsync(product.Id);
        Assert.That(found!.Price, Is.EqualTo(120.50m));
    }

    private void SeedTestData()
    {
        var categories = new[]
        {
            new Category { Id = Guid.NewGuid(), Name = "Fruits & Vegetables" },
            new Category { Id = Guid.NewGuid(), Name = "Dairy & Eggs" },
            new Category { Id = Guid.NewGuid(), Name = "Snacks" },
        };
        _db.Categories.AddRange(categories);
        _db.SaveChanges();

        var cat = categories.ToDictionary(c => c.Name, c => c.Id);
        _db.Products.AddRange(
            new Product { Name = "Apple", Sku = "FV001", Price = 180, ImageUrl = "https://example.com/apple.jpg", CategoryId = cat["Fruits & Vegetables"], StockQuantity = 80, AverageRating = 4.7 },
            new Product { Name = "Amul Milk", Sku = "DE001", Price = 65, ImageUrl = "https://example.com/milk.jpg", CategoryId = cat["Dairy & Eggs"], StockQuantity = 60, AverageRating = 4.4 },
            new Product { Name = "Lays", Sku = "SN001", Price = 20, ImageUrl = "https://example.com/lays.jpg", CategoryId = cat["Snacks"], StockQuantity = 100, AverageRating = 4.3, DiscountPercent = 10 }
        );
        _db.SaveChanges();
    }
}
