using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Backend.NUnitTests.Controllers;

[TestFixture]
public class CouponsControllerTests
{
    private AppDbContext _db = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private void AddCoupon(string code, string type, decimal value, decimal minOrder = 0, int limit = 100, int used = 0, bool active = true)
    {
        _db.Coupons.Add(new Coupon
        {
            Code = code, DiscountType = type, DiscountValue = value,
            MinOrderAmount = minOrder, UsageLimit = limit, UsedCount = used, IsActive = active
        });
        _db.SaveChanges();
    }

    [Test]
    public async Task Validate_ValidPercentageCoupon_ReturnsCorrectDiscount()
    {
        AddCoupon("SAVE10", "Percentage", 10, minOrder: 100);
        var ctrl = new CouponsController(_db);

        var result = await ctrl.Validate(new CouponValidateRequest("SAVE10", 500)) as OkObjectResult;
        var val = result!.Value as CouponValidateResponse;

        Assert.That(val!.Valid, Is.True);
        Assert.That(val.DiscountAmount, Is.EqualTo(50m)); // 10% of 500
    }

    [Test]
    public async Task Validate_ValidFlatCoupon_ReturnsCorrectDiscount()
    {
        AddCoupon("FLAT100", "Flat", 100);
        var ctrl = new CouponsController(_db);

        var result = await ctrl.Validate(new CouponValidateRequest("FLAT100", 500)) as OkObjectResult;
        var val = result!.Value as CouponValidateResponse;

        Assert.That(val!.Valid, Is.True);
        Assert.That(val.DiscountAmount, Is.EqualTo(100m));
    }

    [Test]
    public async Task Validate_FlatCoupon_CapsAtOrderAmount()
    {
        AddCoupon("BIGFLAT", "Flat", 1000);
        var ctrl = new CouponsController(_db);

        var result = await ctrl.Validate(new CouponValidateRequest("BIGFLAT", 200)) as OkObjectResult;
        var val = result!.Value as CouponValidateResponse;

        Assert.That(val!.Valid, Is.True);
        Assert.That(val.DiscountAmount, Is.EqualTo(200m)); // capped at order amount
    }

    [Test]
    public async Task Validate_UnknownCode_ReturnsFalse()
    {
        var ctrl = new CouponsController(_db);
        var result = await ctrl.Validate(new CouponValidateRequest("GHOST", 500)) as OkObjectResult;
        var val = result!.Value as CouponValidateResponse;

        Assert.That(val!.Valid, Is.False);
    }

    [Test]
    public async Task Validate_BelowMinimumOrder_ReturnsFalse()
    {
        AddCoupon("MIN500", "Flat", 50, minOrder: 500);
        var ctrl = new CouponsController(_db);

        var result = await ctrl.Validate(new CouponValidateRequest("MIN500", 100)) as OkObjectResult;
        var val = result!.Value as CouponValidateResponse;

        Assert.That(val!.Valid, Is.False);
    }

    [Test]
    public async Task Validate_UsageLimitReached_ReturnsFalse()
    {
        AddCoupon("MAXED", "Flat", 20, limit: 5, used: 5);
        var ctrl = new CouponsController(_db);

        var result = await ctrl.Validate(new CouponValidateRequest("MAXED", 500)) as OkObjectResult;
        var val = result!.Value as CouponValidateResponse;

        Assert.That(val!.Valid, Is.False);
    }

    [Test]
    public async Task Validate_InactiveCoupon_ReturnsFalse()
    {
        AddCoupon("DEAD", "Flat", 50, active: false);
        var ctrl = new CouponsController(_db);

        var result = await ctrl.Validate(new CouponValidateRequest("DEAD", 500)) as OkObjectResult;
        var val = result!.Value as CouponValidateResponse;

        Assert.That(val!.Valid, Is.False);
    }

    [Test]
    public async Task Validate_CaseInsensitiveCode_Works()
    {
        AddCoupon("SAVE20", "Percentage", 20);
        var ctrl = new CouponsController(_db);

        var result = await ctrl.Validate(new CouponValidateRequest("save20", 500)) as OkObjectResult;
        var val = result!.Value as CouponValidateResponse;

        Assert.That(val!.Valid, Is.True);
    }

    [Test]
    public async Task GetAll_ReturnsOnlyActiveCoupons()
    {
        AddCoupon("ACTIVE", "Flat", 10, active: true);
        AddCoupon("INACTIVE", "Flat", 10, active: false);
        var ctrl = new CouponsController(_db);

        var result = await ctrl.GetAll() as OkObjectResult;
        var coupons = result!.Value as IEnumerable<CouponDto>;

        Assert.That(coupons!.Count(), Is.EqualTo(1));
        Assert.That(coupons!.First().Code, Is.EqualTo("ACTIVE"));
    }
}
