using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.LeaveBalances;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class LeaveBalanceServiceTests
{
    private static async Task<(LeaveBalanceService Service, ApplicationDbContext Context)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new LeaveBalanceService(context);
        return (service, context);
    }

    [Fact]
    public async Task GetMyBalancesAsync_ShouldReturnBalances()
    {
        var (service, context) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var emp = TestDataFactory.CreateEmployee(userId: userId);
        var lt = TestDataFactory.CreateLeaveType();
        var balance = TestDataFactory.CreateLeaveBalance(emp.Id, lt.Id);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        context.LeaveBalances.Add(balance);
        await context.SaveChangesAsync();

        var result = await service.GetMyBalancesAsync(userId);

        result.Should().HaveCount(1);
        result[0].LeaveTypeName.Should().Be(lt.Name);
    }

    [Fact]
    public async Task GetMyBalancesAsync_ShouldThrow_WhenEmployeeNotFound()
    {
        var (service, _) = await CreateServiceAsync();

        var act = () => service.GetMyBalancesAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetEmployeeBalancesAsync_ShouldReturnBalancesForHR()
    {
        var (service, context) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        var lt = TestDataFactory.CreateLeaveType();
        var balance = TestDataFactory.CreateLeaveBalance(emp.Id, lt.Id, totalDays: 12, usedDays: 3);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        context.LeaveBalances.Add(balance);
        await context.SaveChangesAsync();

        var result = await service.GetEmployeeBalancesAsync(emp.Id);

        result.Should().HaveCount(1);
        result[0].RemainingDays.Should().Be(9);
    }

    [Fact]
    public async Task AdjustBalanceAsync_ShouldIncreaseExistingBalance()
    {
        var (service, context) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        var lt = TestDataFactory.CreateLeaveType();
        var balance = TestDataFactory.CreateLeaveBalance(emp.Id, lt.Id, totalDays: 12);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        context.LeaveBalances.Add(balance);
        await context.SaveChangesAsync();

        var request = new AdjustLeaveBalanceRequest(emp.Id, lt.Id, 2026, 3, "Annual adjustment");
        await service.AdjustBalanceAsync(request);

        var updated = await context.LeaveBalances.FirstAsync(lb => lb.EmployeeId == emp.Id);
        updated.TotalDays.Should().Be(15);
    }

    [Fact]
    public async Task AdjustBalanceAsync_ShouldCreateBalance_WhenNotExists()
    {
        var (service, context) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        var lt = TestDataFactory.CreateLeaveType();
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        await context.SaveChangesAsync();

        var request = new AdjustLeaveBalanceRequest(emp.Id, lt.Id, 2026, 10, "New employee");
        await service.AdjustBalanceAsync(request);

        var created = await context.LeaveBalances.FirstOrDefaultAsync(lb => lb.EmployeeId == emp.Id);
        created.Should().NotBeNull();
        created!.TotalDays.Should().Be(10);
    }

    [Fact]
    public async Task InitializeYearlyBalancesAsync_ShouldCreateForAllLeaveTypes()
    {
        var (service, context) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        var lt1 = TestDataFactory.CreateLeaveType(name: "Annual", code: "AL", daysPerYear: 12);
        var lt2 = TestDataFactory.CreateLeaveType(name: "Sick", code: "SL", daysPerYear: 14);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt1);
        context.LeaveTypes.Add(lt2);
        await context.SaveChangesAsync();

        await service.InitializeYearlyBalancesAsync(emp.Id, 2026);

        var balances = await context.LeaveBalances.Where(lb => lb.EmployeeId == emp.Id).ToListAsync();
        balances.Should().HaveCount(2);
        balances.Should().ContainSingle(b => b.LeaveTypeId == lt1.Id && b.TotalDays == 12);
        balances.Should().ContainSingle(b => b.LeaveTypeId == lt2.Id && b.TotalDays == 14);
    }
}
