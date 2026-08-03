using AIHelpdesk.Contracts.Tickets;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class AgentAssignmentServiceTests
{
    private static (AgentAssignmentService Service, ApplicationDbContext Context) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);
        return (new AgentAssignmentService(context), context);
    }

    private static async Task<(Guid UserId, Guid DepartmentId)> SeedUserAndDepartmentAsync(ApplicationDbContext context)
    {
        var user = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        var department = TestDataFactory.CreateDepartment($"Dept-{Guid.NewGuid()}");
        context.Users.Add(user);
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        return (user.Id, department.Id);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAssignment_WithZeroLoad()
    {
        var (service, context) = CreateService();
        var (userId, departmentId) = await SeedUserAndDepartmentAsync(context);

        var result = await service.CreateAsync(new CreateAgentAssignmentRequest(userId, departmentId, 10));

        result.CurrentLoad.Should().Be(0);
        result.MaxTickets.Should().Be(10);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateMaxTicketsAndActiveFlag()
    {
        var (service, context) = CreateService();
        var (userId, departmentId) = await SeedUserAndDepartmentAsync(context);
        var created = await service.CreateAsync(new CreateAgentAssignmentRequest(userId, departmentId, 10));

        var updated = await service.UpdateAsync(created.Id, new UpdateAgentAssignmentRequest(20, false));

        updated.MaxTickets.Should().Be(20);
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _) = CreateService();

        var act = () => service.UpdateAsync(Guid.NewGuid(), new UpdateAgentAssignmentRequest(5, true));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete_AndExcludeFromGetAll()
    {
        var (service, context) = CreateService();
        var (userId, departmentId) = await SeedUserAndDepartmentAsync(context);
        var created = await service.CreateAsync(new CreateAgentAssignmentRequest(userId, departmentId, 10));

        await service.DeleteAsync(created.Id);

        var all = await service.GetAllAsync();
        all.Should().NotContain(a => a.Id == created.Id);
    }

    [Fact]
    public async Task GetByDepartmentAsync_ShouldReturnOnlyMatchingDepartment()
    {
        var (service, context) = CreateService();
        var (userId1, departmentId1) = await SeedUserAndDepartmentAsync(context);
        var (userId2, departmentId2) = await SeedUserAndDepartmentAsync(context);
        await service.CreateAsync(new CreateAgentAssignmentRequest(userId1, departmentId1, 10));
        await service.CreateAsync(new CreateAgentAssignmentRequest(userId2, departmentId2, 10));

        var result = await service.GetByDepartmentAsync(departmentId1);

        result.Should().ContainSingle(a => a.UserId == userId1);
    }

    [Fact]
    public async Task GetNextAvailableAgentAsync_ShouldReturnLeastLoadedActiveAgent()
    {
        var (service, context) = CreateService();
        var (userId1, departmentId) = await SeedUserAndDepartmentAsync(context);
        var (userId2, _) = await SeedUserAndDepartmentAsync(context);
        var assignment1 = await service.CreateAsync(new CreateAgentAssignmentRequest(userId1, departmentId, 10));
        await service.CreateAsync(new CreateAgentAssignmentRequest(userId2, departmentId, 10));

        // give agent1 a higher load so agent2 should be picked
        var entity1 = await context.AgentAssignments.FindAsync(assignment1.Id);
        entity1!.CurrentLoad = 5;
        await context.SaveChangesAsync();

        var next = await service.GetNextAvailableAgentAsync(departmentId);

        next.Should().Be(userId2);
    }

    [Fact]
    public async Task GetNextAvailableAgentAsync_ShouldExcludeAgentsAtMaxCapacity()
    {
        var (service, context) = CreateService();
        var (userId, departmentId) = await SeedUserAndDepartmentAsync(context);
        var assignment = await service.CreateAsync(new CreateAgentAssignmentRequest(userId, departmentId, 5));
        var entity = await context.AgentAssignments.FindAsync(assignment.Id);
        entity!.CurrentLoad = 5; // at capacity
        await context.SaveChangesAsync();

        var next = await service.GetNextAvailableAgentAsync(departmentId);

        next.Should().BeNull();
    }

    [Fact]
    public async Task GetNextAvailableAgentAsync_ShouldExcludeInactiveAgents()
    {
        var (service, context) = CreateService();
        var (userId, departmentId) = await SeedUserAndDepartmentAsync(context);
        var assignment = await service.CreateAsync(new CreateAgentAssignmentRequest(userId, departmentId, 10));
        await service.UpdateAsync(assignment.Id, new UpdateAgentAssignmentRequest(10, false));

        var next = await service.GetNextAvailableAgentAsync(departmentId);

        next.Should().BeNull();
    }
}
