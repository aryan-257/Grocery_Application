using Backend.Data;
using Backend.Hubs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Backend.NUnitTests.Services;

[TestFixture]
public class NotificationServiceTests
{
    private AppDbContext _db = null!;
    private Mock<IHubContext<NotificationHub>> _hubMock = null!;
    private Mock<IClientProxy> _proxyMock = null!;
    private NotificationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);

        _proxyMock = new Mock<IClientProxy>();
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_proxyMock.Object);

        _hubMock = new Mock<IHubContext<NotificationHub>>();
        _hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        _sut = new NotificationService(_db, _hubMock.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task SendToUserAsync_PersistsNotificationToDatabase()
    {
        var userId = Guid.NewGuid();
        await _sut.SendToUserAsync(userId, "Order Shipped", "Your order is on the way", "info");

        var saved = await _db.Notifications.FirstOrDefaultAsync(n => n.UserId == userId);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Title, Is.EqualTo("Order Shipped"));
        Assert.That(saved.Message, Is.EqualTo("Your order is on the way"));
    }

    [Test]
    public async Task SendToUserAsync_SendsSignalRToCorrectGroup()
    {
        var userId = Guid.NewGuid();
        await _sut.SendToUserAsync(userId, "Title", "Msg");

        _hubMock.Verify(h => h.Clients.Group($"user:{userId}"), Times.Once);
        _proxyMock.Verify(p => p.SendCoreAsync("notification", It.IsAny<object[]>(), default), Times.Once);
    }

    [Test]
    public async Task SendToUserAsync_DefaultType_IsInfo()
    {
        var userId = Guid.NewGuid();
        await _sut.SendToUserAsync(userId, "T", "M");

        var saved = await _db.Notifications.FirstAsync(n => n.UserId == userId);
        Assert.That(saved.Type, Is.EqualTo("info"));
    }

    [Test]
    [TestCase("success")]
    [TestCase("warning")]
    [TestCase("error")]
    [TestCase("order")]
    public async Task SendToUserAsync_CustomType_IsSaved(string type)
    {
        var userId = Guid.NewGuid();
        await _sut.SendToUserAsync(userId, "T", "M", type);

        var saved = await _db.Notifications.FirstAsync(n => n.UserId == userId);
        Assert.That(saved.Type, Is.EqualTo(type));
    }

    [Test]
    public async Task SendToUserAsync_WithLink_SavesLink()
    {
        var userId = Guid.NewGuid();
        await _sut.SendToUserAsync(userId, "T", "M", "info", "/orders/123");

        var saved = await _db.Notifications.FirstAsync(n => n.UserId == userId);
        Assert.That(saved.Link, Is.EqualTo("/orders/123"));
    }

    [Test]
    public async Task SendToRoleAsync_SendsToRoleGroup_NotPersisted()
    {
        await _sut.SendToRoleAsync("Admin", "New Order", "Order placed");

        _hubMock.Verify(h => h.Clients.Group("role:Admin"), Times.Once);
        Assert.That(_db.Notifications.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task SendToRoleAsync_MultipleRoles_EachGetsSeparateCall()
    {
        await _sut.SendToRoleAsync("Admin", "T", "M");
        await _sut.SendToRoleAsync("StoreManager", "T", "M");

        _hubMock.Verify(h => h.Clients.Group("role:Admin"), Times.Once);
        _hubMock.Verify(h => h.Clients.Group("role:StoreManager"), Times.Once);
    }
}
