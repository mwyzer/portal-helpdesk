using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class AuditLogServiceTests
{
    private static (AuditLogService Service, ApplicationDbContext Context) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);
        return (new AuditLogService(context), context);
    }

    private static AuditLog MakeLog(AuditAction action, string entityName, Guid? userId = null, DateTime? timestamp = null) => new()
    {
        Action = action,
        EntityName = entityName,
        EntityId = Guid.NewGuid().ToString(),
        Changes = "{}",
        UserId = userId,
        Timestamp = timestamp ?? DateTime.UtcNow
    };

    [Fact]
    public async Task GetAuditLogsAsync_ReturnsNewestFirst_WithPagination()
    {
        var (service, context) = CreateService();
        var older = MakeLog(AuditAction.Create, "Ticket", timestamp: DateTime.UtcNow.AddHours(-2));
        var newer = MakeLog(AuditAction.Create, "Ticket", timestamp: DateTime.UtcNow.AddHours(-1));
        context.AuditLogs.AddRange(older, newer);
        await context.SaveChangesAsync();

        var result = await service.GetAuditLogsAsync(page: 1, pageSize: 1, entityName: null, userId: null, action: null, from: null, to: null);

        result.TotalCount.Should().Be(2);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(newer.Id);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FiltersByEntityNameUserIdAndAction()
    {
        var (service, context) = CreateService();
        var userId = Guid.NewGuid();
        var match = MakeLog(AuditAction.Update, "Ticket", userId);
        context.AuditLogs.AddRange(
            match,
            MakeLog(AuditAction.Update, "Employee", userId),
            MakeLog(AuditAction.Create, "Ticket", userId),
            MakeLog(AuditAction.Update, "Ticket", Guid.NewGuid()));
        await context.SaveChangesAsync();

        var result = await service.GetAuditLogsAsync(1, 20, entityName: "Ticket", userId: userId, action: "Update", from: null, to: null);

        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(match.Id);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FiltersByDateRange()
    {
        var (service, context) = CreateService();
        var inRange = MakeLog(AuditAction.Create, "Ticket", timestamp: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc));
        var beforeRange = MakeLog(AuditAction.Create, "Ticket", timestamp: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        context.AuditLogs.AddRange(inRange, beforeRange);
        await context.SaveChangesAsync();

        var result = await service.GetAuditLogsAsync(
            1, 20, entityName: null, userId: null, action: null,
            from: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            to: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc));

        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(inRange.Id);
    }
}
