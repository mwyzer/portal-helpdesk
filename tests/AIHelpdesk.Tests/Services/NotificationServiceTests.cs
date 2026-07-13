using AIHelpdesk.Contracts.Notifications;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class NotificationServiceTests
{
    private static async Task<(NotificationService Service, ApplicationDbContext Context)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new NotificationService(context);
        return (service, context);
    }

    [Fact]
    public async Task CreateNotificationAsync_ShouldCreate()
    {
        var (service, context) = await CreateServiceAsync();
        var userId = Guid.NewGuid();

        await service.CreateNotificationAsync(userId, "Test Title", "Test Body", "Info", "LeaveRequest", null);

        var notifications = await context.Notifications.ToListAsync();
        notifications.Should().HaveCount(1);
        notifications[0].Title.Should().Be("Test Title");
        notifications[0].UserId.Should().Be(userId);
        notifications[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task GetNotificationsAsync_ShouldReturnUserNotifications()
    {
        var (service, context) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        context.Notifications.Add(TestDataFactory.CreateNotification(userId, "N1", isRead: false));
        context.Notifications.Add(TestDataFactory.CreateNotification(userId, "N2", isRead: true));
        context.Notifications.Add(TestDataFactory.CreateNotification(Guid.NewGuid(), "N3")); // other user
        await context.SaveChangesAsync();

        var result = await service.GetNotificationsAsync(userId, 1, 10, null);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task GetNotificationsAsync_ShouldFilterByReadStatus()
    {
        var (service, context) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        context.Notifications.Add(TestDataFactory.CreateNotification(userId, "Read", isRead: true));
        context.Notifications.Add(TestDataFactory.CreateNotification(userId, "Unread", isRead: false));
        await context.SaveChangesAsync();

        var unreadResult = await service.GetNotificationsAsync(userId, 1, 10, false);
        unreadResult.Items.Should().HaveCount(1);
        unreadResult.Items[0].Title.Should().Be("Unread");

        var readResult = await service.GetNotificationsAsync(userId, 1, 10, true);
        readResult.Items.Should().HaveCount(1);
        readResult.Items[0].Title.Should().Be("Read");
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldSetIsRead()
    {
        var (service, context) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var notif = TestDataFactory.CreateNotification(userId, isRead: false);
        context.Notifications.Add(notif);
        await context.SaveChangesAsync();

        await service.MarkAsReadAsync(notif.Id, userId);

        var updated = await context.Notifications.FindAsync(notif.Id);
        updated!.IsRead.Should().BeTrue();
        updated.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _) = await CreateServiceAsync();

        var act = () => service.MarkAsReadAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldMarkAllUnread()
    {
        var (service, context) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        context.Notifications.Add(TestDataFactory.CreateNotification(userId, "N1", isRead: false));
        context.Notifications.Add(TestDataFactory.CreateNotification(userId, "N2", isRead: false));
        context.Notifications.Add(TestDataFactory.CreateNotification(userId, "N3", isRead: true));
        await context.SaveChangesAsync();

        await service.MarkAllAsReadAsync(userId);

        var unreadCount = await context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        unreadCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnCorrectCount()
    {
        var (service, context) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        context.Notifications.Add(TestDataFactory.CreateNotification(userId, isRead: false));
        context.Notifications.Add(TestDataFactory.CreateNotification(userId, isRead: false));
        context.Notifications.Add(TestDataFactory.CreateNotification(userId, isRead: true));
        await context.SaveChangesAsync();

        var count = await service.GetUnreadCountAsync(userId);

        count.Should().Be(2);
    }
}
