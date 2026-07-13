using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Domain.Common;
using Bogus;
using Microsoft.AspNetCore.Identity;

namespace AIHelpdesk.Tests;

public static class TestDataFactory
{
    private static readonly Faker Faker = new();

    // ── Phase 1 factories ──

    public static ApplicationUser CreateUser(
        string email = "test@example.com",
        string fullName = "Test User",
        string? nik = "NIK-001",
        bool isActive = true)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            NIK = nik,
            IsActive = isActive,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public static ApplicationRole CreateRole(string name = "Test Role", string? description = "A test role")
    {
        return new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public static Department CreateDepartment(string name = "IT", string code = "IT")
    {
        return new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static Position CreatePosition(Guid departmentId, string name = "Developer")
    {
        return new Position
        {
            Id = Guid.NewGuid(),
            Name = name,
            DepartmentId = departmentId,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static Permission CreatePermission(string name = "users.read", string group = "Users")
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            Group = group,
            Description = $"Permission to {name}",
        };
    }

    public static RefreshToken CreateRefreshToken(Guid userId, bool isRevoked = false)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = isRevoked,
        };
    }

    // ── Phase 2 factories ──

    public static Employee CreateEmployee(
        Guid? userId = null,
        string employeeNo = "EMP-001",
        string fullName = "John Doe",
        string email = "john@example.com",
        string? phone = "+62812345678",
        Guid? departmentId = null,
        Guid? positionId = null,
        Guid? managerId = null,
        EmploymentStatus employmentStatus = EmploymentStatus.Active,
        string? workLocation = "Jakarta")
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmployeeNo = employeeNo,
            FullName = fullName,
            Email = email,
            Phone = phone,
            JoinDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
            DepartmentId = departmentId,
            PositionId = positionId,
            ManagerId = managerId,
            EmploymentStatus = employmentStatus,
            WorkLocation = workLocation,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static LeaveType CreateLeaveType(
        string name = "Annual Leave",
        string code = "AL",
        int daysPerYear = 12,
        bool isPaid = true,
        int minServiceMonths = 0,
        bool requiresAttachment = false,
        bool skipManagerApproval = false)
    {
        return new LeaveType
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            DaysPerYear = daysPerYear,
            IsPaid = isPaid,
            IsActive = true,
            MinServiceMonths = minServiceMonths,
            RequiresAttachment = requiresAttachment,
            SkipManagerApproval = skipManagerApproval,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static LeaveBalance CreateLeaveBalance(
        Guid employeeId,
        Guid leaveTypeId,
        int year = 2026,
        int totalDays = 12,
        int usedDays = 0,
        int pendingDays = 0)
    {
        return new LeaveBalance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            TotalDays = totalDays,
            UsedDays = usedDays,
            PendingDays = pendingDays,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static LeaveRequest CreateLeaveRequest(
        Guid employeeId,
        Guid leaveTypeId,
        string reason = "Personal leave",
        LeaveRequestStatus status = LeaveRequestStatus.Draft,
        int days = 2)
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        return new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate,
            EndDate = startDate.AddDays(days - 1),
            TotalDays = days,
            Reason = reason,
            Status = status,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static LeaveApproval CreateLeaveApproval(
        Guid leaveRequestId,
        Guid approverId,
        string approverRole = "Manager",
        ApprovalStatus status = ApprovalStatus.Approved,
        string? note = null)
    {
        return new LeaveApproval
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequestId,
            ApproverId = approverId,
            ApproverRole = approverRole,
            Status = status,
            Note = note,
            ApprovedAt = status != ApprovalStatus.Pending ? DateTime.UtcNow : null,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static Notification CreateNotification(
        Guid userId,
        string title = "Test Notification",
        string body = "Test body",
        NotificationType type = NotificationType.Info,
        bool isRead = false,
        string? referenceType = null,
        Guid? referenceId = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IsRead = isRead,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
