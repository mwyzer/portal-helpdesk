using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIHelpdesk.Infrastructure.Services;

/// <summary>
/// Periodically scans open tickets for SLA breaches/at-risk status and notifies the assigned agent.
/// </summary>
public class TicketSlaBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TicketStatus[] ActiveStatuses =
    [
        TicketStatus.Open, TicketStatus.Assigned, TicketStatus.InProgress, TicketStatus.Reopened
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketSlaBackgroundService> _logger;

    public TicketSlaBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TicketSlaBackgroundService> logger)
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
                await CheckSlaBreachesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SLA breach check failed");
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

    internal async Task CheckSlaBreachesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var candidates = await context.Tickets
            .Where(t => ActiveStatuses.Contains(t.Status)
                && t.SLADeadline != null
                && t.SLAStatus != SLAStatus.Breached)
            .ToListAsync(ct);

        var changed = 0;
        foreach (var ticket in candidates)
        {
            var deadline = ticket.SLADeadline!.Value;

            if (now >= deadline)
            {
                var oldStatus = ticket.SLAStatus.ToString();
                ticket.SLAStatus = SLAStatus.Breached;
                ticket.UpdatedAt = now;

                context.TicketHistories.Add(new TicketHistory
                {
                    TicketId = ticket.Id,
                    Field = "SLAStatus",
                    OldValue = oldStatus,
                    NewValue = nameof(SLAStatus.Breached),
                    ChangedById = ticket.AssignedToId
                });

                context.TicketSLAs.Add(new TicketSLA
                {
                    TicketId = ticket.Id,
                    CategoryId = ticket.CategoryId,
                    TargetHours = (int)Math.Round((deadline - ticket.CreatedAt).TotalHours),
                    BreachedAt = now,
                    NotifiedAt = now
                });

                var notifyUserId = ticket.AssignedAgentId ?? ticket.AssignedToId;
                await notifications.CreateNotificationAsync(
                    notifyUserId,
                    "SLA Breached",
                    $"Ticket \"{ticket.Title}\" has breached its SLA deadline.",
                    "TicketSlaBreach",
                    "Ticket",
                    ticket.Id);

                changed++;
            }
            else if (ticket.SLAStatus == SLAStatus.OnTrack)
            {
                var totalWindowHours = (deadline - ticket.CreatedAt).TotalHours;
                var elapsedHours = (now - ticket.CreatedAt).TotalHours;

                if (totalWindowHours > 0 && elapsedHours / totalWindowHours >= 0.8)
                {
                    ticket.SLAStatus = SLAStatus.AtRisk;
                    ticket.UpdatedAt = now;
                    changed++;
                }
            }
        }

        if (changed > 0)
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("SLA check updated {Count} ticket(s)", changed);
        }
    }
}
