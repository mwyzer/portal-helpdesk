using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class AuditSaveChangesInterceptorTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .AddInterceptors(new AuditSaveChangesInterceptor())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SaveChanges_OnInsert_WritesCreateAuditLog()
    {
        var context = CreateContext();
        var department = TestDataFactory.CreateDepartment("Engineering", "ENG");

        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var log = await context.AuditLogs.SingleAsync();
        log.Action.Should().Be(AuditAction.Create);
        log.EntityName.Should().Be(nameof(Department));
        log.EntityId.Should().Be(department.Id.ToString());
    }

    [Fact]
    public async Task SaveChanges_OnPropertyUpdate_WritesUpdateAuditLogWithOldAndNewValues()
    {
        var context = CreateContext();
        var department = TestDataFactory.CreateDepartment("Engineering", "ENG");
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        department.Name = "Platform Engineering";
        await context.SaveChangesAsync();

        var log = await context.AuditLogs
            .Where(a => a.Action == AuditAction.Update)
            .SingleAsync();
        log.EntityId.Should().Be(department.Id.ToString());
        log.Changes.Should().Contain("Engineering").And.Contain("Platform Engineering");
    }

    [Fact]
    public async Task SaveChanges_OnSoftDelete_WritesDeleteAuditLogNotUpdate()
    {
        var context = CreateContext();
        var department = TestDataFactory.CreateDepartment("Engineering", "ENG");
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        department.IsDeleted = true;
        await context.SaveChangesAsync();

        var log = await context.AuditLogs
            .Where(a => a.EntityId == department.Id.ToString() && a.Action != AuditAction.Create)
            .SingleAsync();
        log.Action.Should().Be(AuditAction.Delete);
    }

    [Fact]
    public async Task SaveChanges_OnNoOpSave_WritesNoUpdateLog()
    {
        var context = CreateContext();
        var department = TestDataFactory.CreateDepartment("Engineering", "ENG");
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        context.Departments.Update(department);
        await context.SaveChangesAsync();

        var updateLogs = await context.AuditLogs.Where(a => a.Action == AuditAction.Update).ToListAsync();
        updateLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChanges_OnRefreshTokenMutation_WritesNoAuditLog()
    {
        var context = CreateContext();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = "token-value",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await context.SaveChangesAsync();

        var refreshTokenLogs = await context.AuditLogs.Where(a => a.EntityName == nameof(RefreshToken)).ToListAsync();
        refreshTokenLogs.Should().BeEmpty();
    }
}
