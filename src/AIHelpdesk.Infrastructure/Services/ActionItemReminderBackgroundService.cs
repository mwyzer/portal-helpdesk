using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIHelpdesk.Infrastructure.Services;

/// <summary>
/// Periodically scans open action items for overdue due dates and notifies the assignee once
/// (tracked via <see cref="Domain.Entities.ActionItem.OverdueNotifiedAt"/> to avoid re-notifying
/// on every scan).
/// </summary>
public class ActionItemReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);
    private static readonly ActionItemStatus[] ActiveStatuses = [ActionItemStatus.Open, ActionItemStatus.InProgress];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ActionItemReminderBackgroundService> _logger;

    public ActionItemReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ActionItemReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOverdueItemsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Overdue action item check failed");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // shutting down
            }
        }
    }

    internal async Task CheckOverdueItemsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var overdueItems = await context.ActionItems
            .Where(a => ActiveStatuses.Contains(a.Status) && a.DueDate < now && a.OverdueNotifiedAt == null)
            .ToListAsync(ct);

        foreach (var item in overdueItems)
        {
            item.OverdueNotifiedAt = now;

            await notifications.CreateNotificationAsync(
                item.AssignedToId,
                "Action Item Overdue",
                $"Action item \"{item.Title}\" was due on {item.DueDate:dd MMM yyyy} and is now overdue.",
                "ActionItemOverdue",
                "ActionItem",
                item.Id);
        }

        if (overdueItems.Count > 0)
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Sent overdue reminders for {Count} action item(s)", overdueItems.Count);
        }
    }
}
