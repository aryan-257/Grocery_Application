using System.Security.Claims;
using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Backend.NUnitTests.Controllers;

[TestFixture]
public class OrdersControllerTests
{
    private AppDbContext _db = null!;
    private NotificationService _notif = null!;
    private Mock<IPaymentService> _paymentMock = null!;
    private EmailService _email = null!;
    private Guid _userId;

    [SetUp]
    public void SetUp()
    {
        _userId = Guid.NewGuid();

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);

        var proxyMock = new Mock<IClientProxy>();
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(proxyMock.Object);
        var hubMock = new Mock<IHubContext<Backend.Hubs.NotificationHub>>();
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);
        _notif = new NotificationService(_db, hubMock.Object);

        _paymentMock = new Mock<IPaymentService>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Email:Host"] = "" })
            .Build();
        _email = new EmailService(config, Mock.Of<ILogger<EmailService>>());
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private OrdersController CreateController(string role = "Customer")
    {
        var ctrl = new OrdersController(_db, _notif, _paymentMock.Object, _email);
        var claims = new[]
        {
            new Claim("sub", _userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
        return ctrl;
    }

    private Order SeedOrder(string status = "Pending", Guid? customerId = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId ?? _userId,
            Status = status,
            SubTotal = 280, DeliveryFee = 49, TaxAmount = 14, TotalAmount = 343,
            DeliveryAddress = "123 Test Street",
            Items = new List<OrderItem>
            {
                new() { ProductId = Guid.NewGuid(), ProductName = "Atta", Quantity = 1, UnitPrice = 280 }
            }
        };
        _db.Orders.Add(order);
        _db.SaveChanges();
        return order;
    }

    [Test]
    public async Task GetOrders_Customer_SeesOnlyOwnOrders()
    {
        SeedOrder();
        SeedOrder(customerId: Guid.NewGuid()); // another user

        var result = await CreateController().GetOrders() as OkObjectResult;
        var orders = result!.Value as IEnumerable<OrderDto>;

        Assert.That(orders!.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetOrders_Admin_SeesAllOrders()
    {
        SeedOrder();
        SeedOrder(customerId: Guid.NewGuid());

        var result = await CreateController("Admin").GetOrders() as OkObjectResult;
        var orders = result!.Value as IEnumerable<OrderDto>;

        Assert.That(orders!.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetOrder_ValidId_ReturnsCorrectOrder()
    {
        var order = SeedOrder();

        var result = await CreateController().GetOrder(order.Id) as OkObjectResult;
        var dto = result!.Value as OrderDto;

        Assert.That(dto!.Id, Is.EqualTo(order.Id.ToString()));
        Assert.That(dto.DeliveryAddress, Is.EqualTo("123 Test Street"));
    }

    [Test]
    public async Task GetOrder_OtherUsersOrder_ReturnsForbid()
    {
        var order = SeedOrder(customerId: Guid.NewGuid());

        var result = await CreateController().GetOrder(order.Id);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetOrder_NonExistentId_ReturnsNotFound()
    {
        var result = await CreateController().GetOrder(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task CreateOrder_EmptyCart_ReturnsBadRequest()
    {
        var result = await CreateController().CreateOrder(new CreateOrderRequest("123 St", null, null));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateStatus_NonExistentOrder_ReturnsNotFound()
    {
        var result = await CreateController("Admin").UpdateStatus(Guid.NewGuid(), new UpdateOrderStatusRequest("Shipped"));

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task UpdateStatus_ValidOrder_ChangesStatus()
    {
        var order = SeedOrder("Processing");

        await CreateController("Admin").UpdateStatus(order.Id, new UpdateOrderStatusRequest("Shipped"));

        var updated = await _db.Orders.FindAsync(order.Id);
        Assert.That(updated!.Status, Is.EqualTo("Shipped"));
    }

    [Test]
    public async Task UpdateStatus_Delivered_SetsDeliveredAt()
    {
        var order = SeedOrder("OutForDelivery");

        await CreateController("Admin").UpdateStatus(order.Id, new UpdateOrderStatusRequest("Delivered"));

        var updated = await _db.Orders.FindAsync(order.Id);
        Assert.That(updated!.DeliveredAt, Is.Not.Null);
    }

    [Test]
    public async Task UpdateStatus_Cancelled_DoesNotSetDeliveredAt()
    {
        var order = SeedOrder("Processing");

        await CreateController("Admin").UpdateStatus(order.Id, new UpdateOrderStatusRequest("Cancelled"));

        var updated = await _db.Orders.FindAsync(order.Id);
        Assert.That(updated!.DeliveredAt, Is.Null);
        Assert.That(updated.Status, Is.EqualTo("Cancelled"));
    }
}
