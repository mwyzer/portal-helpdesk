using AIHelpdesk.Contracts.LeaveTypes;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class LeaveTypeServiceTests
{
    private static async Task<(LeaveTypeService Service, ApplicationDbContext Context)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new LeaveTypeService(context);
        return (service, context);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllActiveLeaveTypes()
    {
        var (service, context) = await CreateServiceAsync();
        context.LeaveTypes.Add(TestDataFactory.CreateLeaveType(name: "Annual Leave", code: "AL"));
        context.LeaveTypes.Add(TestDataFactory.CreateLeaveType(name: "Sick Leave", code: "SL"));
        await context.SaveChangesAsync();

        var result = await service.GetAllAsync();

        result.Should().HaveCount(2);
        result.Should().ContainSingle(lt => lt.Code == "AL");
        result.Should().ContainSingle(lt => lt.Code == "SL");
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateLeaveType()
    {
        var (service, _) = await CreateServiceAsync();

        var request = new CreateLeaveTypeRequest("Maternity Leave", "ML", 90, true, 12, true, true);
        var result = await service.CreateAsync(request);

        result.Name.Should().Be("Maternity Leave");
        result.Code.Should().Be("ML");
        result.DaysPerYear.Should().Be(90);
        result.IsPaid.Should().BeTrue();
        result.MinServiceMonths.Should().Be(12);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateLeaveType()
    {
        var (service, context) = await CreateServiceAsync();
        var lt = TestDataFactory.CreateLeaveType(name: "Old Name", daysPerYear: 10);
        context.LeaveTypes.Add(lt);
        await context.SaveChangesAsync();

        var request = new UpdateLeaveTypeRequest("New Name", 15, true, true, 6, false, false);
        var result = await service.UpdateAsync(lt.Id, request);

        result.Name.Should().Be("New Name");
        result.DaysPerYear.Should().Be(15);
        result.MinServiceMonths.Should().Be(6);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnLeaveType()
    {
        var (service, context) = await CreateServiceAsync();
        var lt = TestDataFactory.CreateLeaveType(name: "Annual Leave");
        context.LeaveTypes.Add(lt);
        await context.SaveChangesAsync();

        var result = await service.GetByIdAsync(lt.Id);

        result.Name.Should().Be("Annual Leave");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _) = await CreateServiceAsync();

        var act = () => service.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete()
    {
        var (service, context) = await CreateServiceAsync();
        var lt = TestDataFactory.CreateLeaveType();
        context.LeaveTypes.Add(lt);
        await context.SaveChangesAsync();

        await service.DeleteAsync(lt.Id);

        var deleted = await context.LeaveTypes.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == lt.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }
}
