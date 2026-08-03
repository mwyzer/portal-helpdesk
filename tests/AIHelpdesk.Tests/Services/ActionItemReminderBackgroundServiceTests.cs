using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIHelpdesk.Tests.Services;

public class ActionItemReminderBackgroundServiceTests
{
    private static (ActionItemReminderBackgroundService Service, ApplicationDbContext Context, IServiceProvider Provider) CreateService()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<INotificationService, NotificationService>();
        var provider = services.BuildServiceProvider();

        var service = new ActionItemReminderBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ActionItemReminderBackgroundService>.Instance);

        return (service, provider.GetRequiredService<ApplicationDbContext>(), provider);
    }

    private static ActionItem CreateActionItem(DateTime dueDate, ActionItemStatus status = ActionItemStatus.Open, DateTime? overdueNotifiedAt = null)
    {
        return new ActionItem
        {
            Title = "Follow up with vendor",
            AssignedToId = Guid.NewGuid(),
            DueDate = dueDate,
            Status = status,
            OverdueNotifiedAt = overdueNotifiedAt
        };
    }

    [Fact]
    public async Task CheckOverdueItemsAsync_ShouldNotify_ForOverdueOpenItem()
    {
        var (service, context, provider) = CreateService();
        var item = CreateActionItem(DateTime.UtcNow.AddDays(-2));
        context.ActionItems.Add(item);
        await context.SaveChangesAsync();

        await service.CheckOverdueItemsAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var verifyContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var updated = await verifyContext.ActionItems.AsNoTracking().FirstAsync(a => a.Id == item.Id);
        updated.OverdueNotifiedAt.Should().NotBeNull();

        var notification = await verifyContext.Notifications.FirstOrDefaultAsync(n => n.UserId == item.AssignedToId);
        notification.Should().NotBeNull();
        notification!.Title.Should().Be("Action Item Overdue");
    }

    [Fact]
    public async Task CheckOverdueItemsAsync_ShouldIgnore_ItemsNotYetDue()
    {
        var (service, context, provider) = CreateService();
        var item = CreateActionItem(DateTime.UtcNow.AddDays(2));
        context.ActionItems.Add(item);
        await context.SaveChangesAsync();

        await service.CheckOverdueItemsAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var verifyContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await verifyContext.ActionItems.AsNoTracking().FirstAsync(a => a.Id == item.Id);
        updated.OverdueNotifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task CheckOverdueItemsAsync_ShouldIgnore_CompletedItems()
    {
        var (service, context, provider) = CreateService();
        var item = CreateActionItem(DateTime.UtcNow.AddDays(-2), status: ActionItemStatus.Completed);
        context.ActionItems.Add(item);
        await context.SaveChangesAsync();

        await service.CheckOverdueItemsAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var verifyContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notification = await verifyContext.Notifications.FirstOrDefaultAsync(n => n.UserId == item.AssignedToId);
        notification.Should().BeNull();
    }

    [Fact]
    public async Task CheckOverdueItemsAsync_ShouldBeIdempotent_ForAlreadyNotifiedItems()
    {
        var (service, context, provider) = CreateService();
        var item = CreateActionItem(DateTime.UtcNow.AddDays(-2), overdueNotifiedAt: DateTime.UtcNow.AddHours(-1));
        context.ActionItems.Add(item);
        await context.SaveChangesAsync();

        await service.CheckOverdueItemsAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var verifyContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = await verifyContext.Notifications.Where(n => n.UserId == item.AssignedToId).ToListAsync();
        notifications.Should().BeEmpty(); // already-notified items are excluded from the scan
    }
}
