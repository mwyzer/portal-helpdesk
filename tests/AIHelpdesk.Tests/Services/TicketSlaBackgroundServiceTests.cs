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

public class TicketSlaBackgroundServiceTests
{
    private static (TicketSlaBackgroundService Service, ApplicationDbContext Context, IServiceProvider Provider) CreateService()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<INotificationService, NotificationService>();
        var provider = services.BuildServiceProvider();

        var service = new TicketSlaBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<TicketSlaBackgroundService>.Instance);

        return (service, provider.GetRequiredService<ApplicationDbContext>(), provider);
    }

    // The background service updates entities through its own DI scope. Reading back through a
    // fresh scope (rather than the Arrange-time context, which still has a stale tracked copy)
    // mirrors how a real request would observe the change.
    private static async Task<Ticket?> ReloadTicketAsync(IServiceProvider provider, Guid ticketId)
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId);
    }

    private static Ticket CreateTicket(DateTime createdAt, DateTime? slaDeadline, TicketStatus status = TicketStatus.Open, SLAStatus slaStatus = SLAStatus.OnTrack)
    {
        return new Ticket
        {
            Title = "Test ticket",
            Description = "Description",
            CategoryId = Guid.NewGuid(),
            Priority = TicketPriority.Normal,
            Status = status,
            AssignedToId = Guid.NewGuid(),
            SubmittedById = Guid.NewGuid(),
            SLADeadline = slaDeadline,
            SLAStatus = slaStatus,
            CreatedAt = createdAt
        };
    }

    [Fact]
    public async Task CheckSlaBreachesAsync_ShouldMarkBreached_WhenDeadlinePassed()
    {
        var (service, context, provider) = CreateService();
        var ticket = CreateTicket(DateTime.UtcNow.AddHours(-25), DateTime.UtcNow.AddHours(-1));
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        await service.CheckSlaBreachesAsync(CancellationToken.None);

        var updated = await ReloadTicketAsync(provider, ticket.Id);
        updated!.SLAStatus.Should().Be(SLAStatus.Breached);

        using var scope = provider.CreateScope();
        var verifyContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var history = await verifyContext.TicketHistories.Where(h => h.TicketId == ticket.Id).ToListAsync();
        history.Should().ContainSingle(h => h.Field == "SLAStatus" && h.NewValue == "Breached");

        var slaRecord = await verifyContext.TicketSLAs.FirstOrDefaultAsync(s => s.TicketId == ticket.Id);
        slaRecord.Should().NotBeNull();
        slaRecord!.BreachedAt.Should().NotBeNull();

        var notification = await verifyContext.Notifications.FirstOrDefaultAsync(n => n.UserId == ticket.AssignedToId);
        notification.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckSlaBreachesAsync_ShouldMarkAtRisk_When80PercentElapsed()
    {
        var (service, context, provider) = CreateService();
        // 24h window, created 20h ago (83% elapsed) — still before deadline
        var ticket = CreateTicket(DateTime.UtcNow.AddHours(-20), DateTime.UtcNow.AddHours(4));
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        await service.CheckSlaBreachesAsync(CancellationToken.None);

        var updated = await ReloadTicketAsync(provider, ticket.Id);
        updated!.SLAStatus.Should().Be(SLAStatus.AtRisk);
    }

    [Fact]
    public async Task CheckSlaBreachesAsync_ShouldIgnore_ResolvedTickets()
    {
        var (service, context, provider) = CreateService();
        var ticket = CreateTicket(DateTime.UtcNow.AddHours(-25), DateTime.UtcNow.AddHours(-1), status: TicketStatus.Resolved);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        await service.CheckSlaBreachesAsync(CancellationToken.None);

        var updated = await ReloadTicketAsync(provider, ticket.Id);
        updated!.SLAStatus.Should().Be(SLAStatus.OnTrack);
    }

    [Fact]
    public async Task CheckSlaBreachesAsync_ShouldBeIdempotent_ForAlreadyBreachedTickets()
    {
        var (service, context, provider) = CreateService();
        var ticket = CreateTicket(DateTime.UtcNow.AddHours(-25), DateTime.UtcNow.AddHours(-1), slaStatus: SLAStatus.Breached);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        await service.CheckSlaBreachesAsync(CancellationToken.None);
        await service.CheckSlaBreachesAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var verifyContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var slaRecords = await verifyContext.TicketSLAs.Where(s => s.TicketId == ticket.Id).ToListAsync();
        slaRecords.Should().BeEmpty(); // already-breached tickets are excluded from the scan entirely
    }
}
