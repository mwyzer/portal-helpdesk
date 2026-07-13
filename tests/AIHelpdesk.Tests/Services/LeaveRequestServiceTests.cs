using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.LeaveRequests;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AIHelpdesk.Tests.Services;

public class LeaveRequestServiceTests
{
    private static async Task<(LeaveRequestService Service, ApplicationDbContext Context, Mock<INotificationService> NotificationMock)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var notificationMock = new Mock<INotificationService>();
        var service = new LeaveRequestService(context, notificationMock.Object);
        return (service, context, notificationMock);
    }

    private static ApplicationUser CreateAppUser(Guid id, string name = "Test User")
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = $"user{id:N}@test.com",
            Email = $"user{id:N}@test.com",
            FullName = name,
            IsActive = true,
        };
    }

    // ── CreateDraftAsync ──

    [Fact]
    public async Task CreateDraftAsync_ShouldCreateDraftLeaveRequest()
    {
        var (service, context, _) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        var lt = TestDataFactory.CreateLeaveType();
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        await context.SaveChangesAsync();

        var request = new CreateLeaveRequest(lt.Id,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6)), "Test", null);

        var result = await service.CreateDraftAsync(emp.Id, request);

        result.Status.Should().Be("Draft");
        result.TotalDays.Should().Be(2);
        result.EmployeeId.Should().Be(emp.Id);
    }

    [Fact]
    public async Task CreateDraftAsync_ShouldThrow_WhenEmployeeNotFound()
    {
        var (service, _, _) = await CreateServiceAsync();

        var act = () => service.CreateDraftAsync(Guid.NewGuid(),
            new CreateLeaveRequest(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow), "Test", null));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── SubmitAsync ──

    [Fact]
    public async Task SubmitAsync_ShouldSubmitAndTransitionToWaitingForManager()
    {
        var (service, context, _) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee(userId: Guid.NewGuid());
        var dept = TestDataFactory.CreateDepartment();
        var pos = TestDataFactory.CreatePosition(dept.Id);
        emp.DepartmentId = dept.Id;
        emp.PositionId = pos.Id;
        var lt = TestDataFactory.CreateLeaveType(daysPerYear: 12);
        var balance = TestDataFactory.CreateLeaveBalance(emp.Id, lt.Id, totalDays: 12, usedDays: 0, pendingDays: 0);
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.Draft, days: 2);
        context.Departments.Add(dept);
        context.Positions.Add(pos);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        context.LeaveBalances.Add(balance);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var result = await service.SubmitAsync(lr.Id, emp.Id);

        result.Status.Should().Be("WaitingForManager");
        var updatedBalance = await context.LeaveBalances.FindAsync(balance.Id);
        updatedBalance!.PendingDays.Should().Be(2);
    }

    [Fact]
    public async Task SubmitAsync_ShouldSkipManagerWhenLeaveTypeAllows()
    {
        var (service, context, _) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee(userId: Guid.NewGuid());
        var lt = TestDataFactory.CreateLeaveType(skipManagerApproval: true);
        var balance = TestDataFactory.CreateLeaveBalance(emp.Id, lt.Id, totalDays: 14);
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.Draft, days: 1);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        context.LeaveBalances.Add(balance);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var result = await service.SubmitAsync(lr.Id, emp.Id);

        result.Status.Should().Be("WaitingForHR");
    }

    [Fact]
    public async Task SubmitAsync_ShouldThrow_WhenInsufficientBalance()
    {
        var (service, context, _) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        var lt = TestDataFactory.CreateLeaveType(daysPerYear: 2);
        var balance = TestDataFactory.CreateLeaveBalance(emp.Id, lt.Id, totalDays: 2, usedDays: 2, pendingDays: 0);
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.Draft, days: 3);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        context.LeaveBalances.Add(balance);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var act = () => service.SubmitAsync(lr.Id, emp.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*balance*");
    }

    [Fact]
    public async Task SubmitAsync_ShouldThrow_WhenNotDraft()
    {
        var (service, context, _) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        var lt = TestDataFactory.CreateLeaveType();
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.WaitingForManager);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var act = () => service.SubmitAsync(lr.Id, emp.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*draft*");
    }

    // ── ApproveAsync (Manager path) ──

    [Fact]
    public async Task ApproveAsync_Manager_ShortLeave_ShouldApproveImmediately()
    {
        var (service, context, _) = await CreateServiceAsync();
        var managerUserId = Guid.NewGuid();
        var managerAppUser = CreateAppUser(managerUserId, "Manager");
        var manager = TestDataFactory.CreateEmployee(userId: managerUserId, fullName: "Manager");
        var dept = TestDataFactory.CreateDepartment();
        var pos = TestDataFactory.CreatePosition(dept.Id);
        var empUserId = Guid.NewGuid();
        var empAppUser = CreateAppUser(empUserId, "Subordinate");
        var emp = TestDataFactory.CreateEmployee(
            userId: empUserId, fullName: "Subordinate",
            departmentId: dept.Id, positionId: pos.Id, managerId: manager.Id);
        emp.Manager = manager;
        var lt = TestDataFactory.CreateLeaveType();
        var balance = TestDataFactory.CreateLeaveBalance(emp.Id, lt.Id, totalDays: 12, pendingDays: 2);
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.WaitingForManager, days: 2);
        lr.LeaveType = lt;
        lr.Employee = emp;
        context.Set<ApplicationUser>().AddRange(managerAppUser, empAppUser);
        context.Departments.Add(dept);
        context.Positions.Add(pos);
        context.Employees.AddRange(manager, emp);
        context.LeaveTypes.Add(lt);
        context.LeaveBalances.Add(balance);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var result = await service.ApproveAsync(lr.Id, managerUserId);

        result.Status.Should().Be("Approved");
        var updated = await context.LeaveBalances.FindAsync(balance.Id);
        updated!.UsedDays.Should().Be(2);
        updated.PendingDays.Should().Be(0);
    }

    [Fact]
    public async Task ApproveAsync_Manager_LongLeave_ShouldGoToWaitingForHR()
    {
        var (service, context, _) = await CreateServiceAsync();
        var managerUserId = Guid.NewGuid();
        var managerAppUser = CreateAppUser(managerUserId, "Manager");
        var manager = TestDataFactory.CreateEmployee(userId: managerUserId, fullName: "Manager");
        var empUserId = Guid.NewGuid();
        var empAppUser = CreateAppUser(empUserId, "Subordinate");
        var emp = TestDataFactory.CreateEmployee(userId: empUserId, fullName: "Subordinate", managerId: manager.Id);
        emp.Manager = manager;
        var lt = TestDataFactory.CreateLeaveType();
        var balance = TestDataFactory.CreateLeaveBalance(emp.Id, lt.Id, totalDays: 12, pendingDays: 5);
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.WaitingForManager, days: 5);
        lr.LeaveType = lt;
        lr.Employee = emp;
        context.Set<ApplicationUser>().AddRange(managerAppUser, empAppUser);
        context.Employees.AddRange(manager, emp);
        context.LeaveTypes.Add(lt);
        context.LeaveBalances.Add(balance);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var result = await service.ApproveAsync(lr.Id, managerUserId);

        result.Status.Should().Be("WaitingForHR");
        var b = await context.LeaveBalances.FindAsync(balance.Id);
        b!.PendingDays.Should().Be(5);
    }

    [Fact]
    public async Task ApproveAsync_ShouldThrow_WhenWrongManager()
    {
        var (service, context, _) = await CreateServiceAsync();
        var correctManagerUserId = Guid.NewGuid();
        var wrongManagerUserId = Guid.NewGuid();
        var correctAppUser = CreateAppUser(correctManagerUserId, "Correct Manager");
        var wrongAppUser = CreateAppUser(wrongManagerUserId, "Wrong Approver");
        var manager = TestDataFactory.CreateEmployee(userId: correctManagerUserId, fullName: "Correct Manager");
        var emp = TestDataFactory.CreateEmployee(userId: Guid.NewGuid(), managerId: manager.Id);
        emp.Manager = manager;
        var lt = TestDataFactory.CreateLeaveType();
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.WaitingForManager);
        lr.LeaveType = lt;
        lr.Employee = emp;
        context.Set<ApplicationUser>().AddRange(correctAppUser, wrongAppUser);
        context.Employees.AddRange(manager, emp);
        context.LeaveTypes.Add(lt);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var act = () => service.ApproveAsync(lr.Id, wrongManagerUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── ApproveAsync (HRD path) ──

    [Fact]
    public async Task ApproveAsync_HRD_ShouldFinalizeApproval()
    {
        var (service, context, _) = await CreateServiceAsync();
        var hrdUserId = Guid.NewGuid();
        var hrdAppUser = CreateAppUser(hrdUserId, "HRD User");
        var empUserId = Guid.NewGuid();
        var empAppUser = CreateAppUser(empUserId, "Employee");
        var emp = TestDataFactory.CreateEmployee(userId: empUserId);
        var lt = TestDataFactory.CreateLeaveType();
        var balance = TestDataFactory.CreateLeaveBalance(emp.Id, lt.Id, totalDays: 12, pendingDays: 5);
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.WaitingForHR, days: 5);
        lr.LeaveType = lt;
        lr.Employee = emp;
        context.Set<ApplicationUser>().AddRange(hrdAppUser, empAppUser);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        context.LeaveBalances.Add(balance);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var result = await service.ApproveAsync(lr.Id, hrdUserId);

        result.Status.Should().Be("Approved");
        var finalBalance = await context.LeaveBalances.FindAsync(balance.Id);
        finalBalance!.UsedDays.Should().Be(5);
        finalBalance.PendingDays.Should().Be(0);
    }

    // ── RejectAsync ──

    [Fact]
    public async Task RejectAsync_ShouldRejectAndReleasePendingBalance()
    {
        var (service, context, _) = await CreateServiceAsync();
        var approverUserId = Guid.NewGuid();
        var approverAppUser = CreateAppUser(approverUserId, "Manager Approver");
        var empUserId = Guid.NewGuid();
        var empAppUser = CreateAppUser(empUserId, "Employee");
        var emp = TestDataFactory.CreateEmployee(userId: empUserId);
        var manager = TestDataFactory.CreateEmployee(userId: approverUserId);
        emp.Manager = manager;
        var lt = TestDataFactory.CreateLeaveType();
        var balance = TestDataFactory.CreateLeaveBalance(emp.Id, lt.Id, totalDays: 12, pendingDays: 3);
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.WaitingForManager, days: 3);
        lr.Employee = emp;
        context.Set<ApplicationUser>().AddRange(approverAppUser, empAppUser);
        context.Employees.AddRange(emp, manager);
        context.LeaveTypes.Add(lt);
        context.LeaveBalances.Add(balance);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var result = await service.RejectAsync(lr.Id, approverUserId, "Not allowed");

        result.Status.Should().Be("Rejected");
        result.RejectionReason.Should().Be("Not allowed");
        var updated = await context.LeaveBalances.FindAsync(balance.Id);
        updated!.PendingDays.Should().Be(0);
    }

    [Fact]
    public async Task RejectAsync_ShouldThrow_WhenNotAwaitingApproval()
    {
        var (service, context, _) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        var lt = TestDataFactory.CreateLeaveType();
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.Approved);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var act = () => service.RejectAsync(lr.Id, Guid.NewGuid(), "Test");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*awaiting approval*");
    }

    // ── CancelAsync ──

    [Fact]
    public async Task CancelAsync_ShouldCancelAndReleaseBalance()
    {
        var (service, context, _) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        var lt = TestDataFactory.CreateLeaveType();
        var balance = TestDataFactory.CreateLeaveBalance(emp.Id, lt.Id, pendingDays: 3);
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.WaitingForManager, days: 3);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        context.LeaveBalances.Add(balance);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var result = await service.CancelAsync(lr.Id, emp.Id);

        result.Status.Should().Be("Cancelled");
        var updated = await context.LeaveBalances.FindAsync(balance.Id);
        updated!.PendingDays.Should().Be(0);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrow_WhenAlreadyApproved()
    {
        var (service, context, _) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        var lt = TestDataFactory.CreateLeaveType();
        var lr = TestDataFactory.CreateLeaveRequest(emp.Id, lt.Id, status: LeaveRequestStatus.Approved);
        context.Employees.Add(emp);
        context.LeaveTypes.Add(lt);
        context.LeaveRequests.Add(lr);
        await context.SaveChangesAsync();

        var act = () => service.CancelAsync(lr.Id, emp.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be cancelled*");
    }
}
