using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class DepartmentServiceTests
{
    private async Task<(DepartmentService, ApplicationDbContext)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new DepartmentService(context);
        return (service, context);
    }

    [Fact]
    public async Task GetDepartmentsAsync_ShouldReturnEmpty_WhenNoDepartments()
    {
        var (service, _) = await CreateServiceAsync();

        var result = await service.GetDepartmentsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateDepartmentAsync_ShouldCreateDepartment()
    {
        var (service, _) = await CreateServiceAsync();

        var result = await service.CreateDepartmentAsync(new("Finance", "FIN"));

        result.Name.Should().Be("Finance");
        result.Code.Should().Be("FIN");
        result.IsActive.Should().BeTrue();
        result.PositionCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateDepartmentAsync_ShouldUpdate()
    {
        var (service, _) = await CreateServiceAsync();
        var created = await service.CreateDepartmentAsync(new("Old Name", "OLD"));

        var updated = await service.UpdateDepartmentAsync(created.Id, new("New Name", "NEW", true));

        updated.Name.Should().Be("New Name");
        updated.Code.Should().Be("NEW");
    }

    [Fact]
    public async Task CreatePositionAsync_ShouldCreateAndBelongToDepartment()
    {
        var (service, _) = await CreateServiceAsync();
        var dept = await service.CreateDepartmentAsync(new("Engineering", "ENG"));

        var pos = await service.CreatePositionAsync(new("Senior Dev", dept.Id));

        pos.Name.Should().Be("Senior Dev");
        pos.DepartmentId.Should().Be(dept.Id);

        var positions = await service.GetPositionsAsync(dept.Id);
        positions.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPositionsAsync_ShouldFilterByDepartment()
    {
        var (service, _) = await CreateServiceAsync();
        var eng = await service.CreateDepartmentAsync(new("Engineering", "ENG"));
        var hr = await service.CreateDepartmentAsync(new("HR", "HR"));

        await service.CreatePositionAsync(new("Engineer", eng.Id));
        await service.CreatePositionAsync(new("Recruiter", hr.Id));

        var engPositions = await service.GetPositionsAsync(eng.Id);
        engPositions.Should().HaveCount(1);
        engPositions[0].Name.Should().Be("Engineer");
    }
}
