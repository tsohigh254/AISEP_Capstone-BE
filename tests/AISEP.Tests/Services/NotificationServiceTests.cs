using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Services;
using AISEP.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace AISEP.Tests.Services;

public class NotificationServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<IAuditService> _audit = new();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new NotificationService(_db, _audit.Object);
    }

    private Notification SeedNotification(int userId, string type = "Info", bool isRead = false, string title = "Test")
    {
        var n = new Notification
        {
            UserID = userId,
            NotificationType = type,
            Title = title,
            Message = "Test message body",
            IsRead = isRead,
            CreatedAt = DateTime.UtcNow
        };
        _db.Notifications.Add(n);
        _db.SaveChanges();
        return n;
    }

    // ── GetMyNotificationsAsync ──────────────────────────────────

    [Fact]
    public async Task GetMyNotificationsAsync_ReturnsOnlyOwnNotifications()
    {
        SeedNotification(userId: 1);
        SeedNotification(userId: 1);
        SeedNotification(userId: 2);

        var result = await _sut.GetMyNotificationsAsync(userId: 1, null, null, 1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMyNotificationsAsync_UnreadOnly_FiltersReadOnes()
    {
        SeedNotification(userId: 1, isRead: false);
        SeedNotification(userId: 1, isRead: true);

        var result = await _sut.GetMyNotificationsAsync(1, unreadOnly: true, null, 1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMyNotificationsAsync_FilterByType_ReturnsMatching()
    {
        SeedNotification(userId: 1, type: "Alert");
        SeedNotification(userId: 1, type: "Info");

        var result = await _sut.GetMyNotificationsAsync(1, null, type: "Alert", 1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].NotificationType.Should().Be("Alert");
    }

    [Fact]
    public async Task GetMyNotificationsAsync_Paging_RespectsPageSize()
    {
        for (int i = 0; i < 5; i++) SeedNotification(userId: 1);

        var result = await _sut.GetMyNotificationsAsync(1, null, null, page: 1, pageSize: 2);

        result.Data!.Items.Should().HaveCount(2);
        result.Data.Paging.TotalItems.Should().Be(5);
    }

    [Fact]
    public async Task GetMyNotificationsAsync_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _sut.GetMyNotificationsAsync(userId: 99, null, null, 1, 10);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Paging.TotalItems.Should().Be(0);
    }

    // ── GetMyNotificationAsync ───────────────────────────────────

    [Fact]
    public async Task GetMyNotificationAsync_WhenNotFound_ReturnsError()
    {
        var result = await _sut.GetMyNotificationAsync(userId: 1, notificationId: 999);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("NOTIFICATION_NOT_FOUND");
    }

    [Fact]
    public async Task GetMyNotificationAsync_WhenNotOwned_ReturnsAccessDenied()
    {
        var n = SeedNotification(userId: 2);

        var result = await _sut.GetMyNotificationAsync(userId: 1, n.NotificationID);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("ACCESS_DENIED");
    }

    [Fact]
    public async Task GetMyNotificationAsync_WhenOwned_ReturnsDto()
    {
        var n = SeedNotification(userId: 1, title: "Hello");

        var result = await _sut.GetMyNotificationAsync(userId: 1, n.NotificationID);

        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("Hello");
    }

    // ── MarkReadAsync ────────────────────────────────────────────

    [Fact]
    public async Task MarkReadAsync_WhenNotFound_ReturnsError()
    {
        var result = await _sut.MarkReadAsync(1, 999, true);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("NOTIFICATION_NOT_FOUND");
    }

    [Fact]
    public async Task MarkReadAsync_WhenNotOwned_ReturnsAccessDenied()
    {
        var n = SeedNotification(userId: 2);

        var result = await _sut.MarkReadAsync(userId: 1, n.NotificationID, true);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("ACCESS_DENIED");
    }

    [Fact]
    public async Task MarkReadAsync_SetTrue_MarksReadAndSetsReadAt()
    {
        var n = SeedNotification(userId: 1, isRead: false);

        var result = await _sut.MarkReadAsync(1, n.NotificationID, isRead: true);

        result.Success.Should().BeTrue();
        var reloaded = await _db.Notifications.FindAsync(n.NotificationID);
        reloaded!.IsRead.Should().BeTrue();
        reloaded.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkReadAsync_SetFalse_ClearsReadAt()
    {
        var n = SeedNotification(userId: 1, isRead: true);
        n.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var result = await _sut.MarkReadAsync(1, n.NotificationID, isRead: false);

        result.Success.Should().BeTrue();
        var reloaded = await _db.Notifications.FindAsync(n.NotificationID);
        reloaded!.IsRead.Should().BeFalse();
        reloaded.ReadAt.Should().BeNull();
    }

    // ── DeleteAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsError()
    {
        var result = await _sut.DeleteAsync(1, 999);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("NOTIFICATION_NOT_FOUND");
    }

    [Fact]
    public async Task DeleteAsync_WhenNotOwned_ReturnsAccessDenied()
    {
        var n = SeedNotification(userId: 2);

        var result = await _sut.DeleteAsync(userId: 1, n.NotificationID);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("ACCESS_DENIED");
    }

    [Fact]
    public async Task DeleteAsync_WhenOwned_RemovesAndLogsAudit()
    {
        var n = SeedNotification(userId: 1);

        var result = await _sut.DeleteAsync(1, n.NotificationID);

        result.Success.Should().BeTrue();
        _db.Notifications.Should().BeEmpty();
        _audit.Verify(a => a.LogAsync("DELETE_NOTIFICATION", "Notification", n.NotificationID, It.IsAny<string>()), Times.Once);
    }

    // ── Boundary Tests ────────────────────────────────────────────

    [Fact]
    public async Task GetMyNotificationsAsync_WithPageSize100_ReturnsAllItemsAtUpperBoundary()
    {
        // Boundary: pageSize=100 (upper boundary of standard paging API)
        for (int i = 0; i < 100; i++) SeedNotification(userId: 1);

        var result = await _sut.GetMyNotificationsAsync(1, null, null, page: 1, pageSize: 100);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(100);
        result.Data.Paging.TotalItems.Should().Be(100);
    }
}
