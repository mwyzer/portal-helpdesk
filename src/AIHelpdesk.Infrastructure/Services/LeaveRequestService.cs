using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.LeaveRequests;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public LeaveRequestService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<LeaveRequestListResponse> GetLeaveRequestsAsync(Guid userId, int page, int pageSize, string? status)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId)
            ?? throw new KeyNotFoundException("Employee profile not found");

        var query = _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.Approvals).ThenInclude(a => a.Approver)
            .Where(lr => lr.EmployeeId == employee.Id);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeaveRequestStatus>(status, out var lrs))
            query = query.Where(lr => lr.Status == lrs);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(lr => lr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(lr => MapToResponse(lr))
            .ToListAsync();

        return new LeaveRequestListResponse(items, totalCount, page, pageSize);
    }

    public async Task<LeaveRequestResponse> GetLeaveRequestAsync(Guid id)
    {
        var lr = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.Approvals).ThenInclude(a => a.Approver)
            .FirstOrDefaultAsync(lr => lr.Id == id);

        if (lr == null)
            throw new KeyNotFoundException("Leave request not found");

        return MapToResponse(lr);
    }

    public async Task<LeaveRequestResponse> CreateDraftAsync(Guid employeeId, CreateLeaveRequest request)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new KeyNotFoundException("Employee not found");

        var totalDays = (request.EndDate.DayNumber - request.StartDate.DayNumber) + 1;

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = totalDays,
            Reason = request.Reason,
            AttachmentUrl = request.AttachmentUrl,
            Status = LeaveRequestStatus.Draft
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();

        return await GetLeaveRequestAsync(leaveRequest.Id);
    }

    public async Task<LeaveRequestResponse> UpdateDraftAsync(Guid id, Guid employeeId, UpdateLeaveRequest request)
    {
        var lr = await _context.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.Id == id && lr.EmployeeId == employeeId);

        if (lr == null)
            throw new KeyNotFoundException("Leave request not found");

        if (lr.Status != LeaveRequestStatus.Draft)
            throw new InvalidOperationException("Only draft leave requests can be edited");

        var totalDays = (request.EndDate.DayNumber - request.StartDate.DayNumber) + 1;

        lr.LeaveTypeId = request.LeaveTypeId;
        lr.StartDate = request.StartDate;
        lr.EndDate = request.EndDate;
        lr.TotalDays = totalDays;
        lr.Reason = request.Reason;
        lr.AttachmentUrl = request.AttachmentUrl;
        lr.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetLeaveRequestAsync(id);
    }

    public async Task<LeaveRequestResponse> SubmitAsync(Guid id, Guid employeeId)
    {
        var lr = await _context.LeaveRequests
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(lr => lr.Id == id && lr.EmployeeId == employeeId);

        if (lr == null)
            throw new KeyNotFoundException("Leave request not found");

        if (lr.Status != LeaveRequestStatus.Draft)
            throw new InvalidOperationException("Only draft requests can be submitted");

        // Validate balance
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb =>
                lb.EmployeeId == employeeId &&
                lb.LeaveTypeId == lr.LeaveTypeId &&
                lb.Year == DateTime.UtcNow.Year);

        if (balance == null || (balance.RemainingDays) < lr.TotalDays)
            throw new InvalidOperationException("Insufficient leave balance");

        // Check service months
        var leaveType = await _context.LeaveTypes.FindAsync(lr.LeaveTypeId);
        if (leaveType != null && leaveType.MinServiceMonths > 0)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee != null)
            {
                var serviceMonths = ((DateTime.UtcNow.Year - employee.JoinDate.Year) * 12) +
                    (DateTime.UtcNow.Month - employee.JoinDate.Month);
                if (serviceMonths < leaveType.MinServiceMonths)
                    throw new InvalidOperationException(
                        $"Employee needs at least {leaveType.MinServiceMonths} months of service for this leave type");
            }
        }

        // Update balance pending
        if (balance != null)
        {
            balance.PendingDays += lr.TotalDays;
        }

        // Determine next status
        if (leaveType?.SkipManagerApproval == true)
        {
            lr.Status = LeaveRequestStatus.WaitingForHR;
        }
        else
        {
            lr.Status = LeaveRequestStatus.WaitingForManager;
        }

        lr.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Notify manager
        var emp = await _context.Employees.Include(e => e.Manager).FirstOrDefaultAsync(e => e.Id == employeeId);
        if (emp?.Manager?.UserId != null)
        {
            await _notificationService.CreateNotificationAsync(
                emp.Manager.UserId.Value,
                "Leave Request Submitted",
                $"{emp.FullName} submitted a leave request ({lr.TotalDays} days)",
                "Info", "LeaveRequest", lr.Id);
        }

        return await GetLeaveRequestAsync(id);
    }

    public async Task<LeaveRequestResponse> ApproveAsync(Guid id, Guid approverId)
    {
        var lr = await _context.LeaveRequests
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id);

        if (lr == null)
            throw new KeyNotFoundException("Leave request not found");

        var approver = await _context.Users.FindAsync(approverId)
            ?? throw new KeyNotFoundException("Approver not found");

        var isManager = lr.Status == LeaveRequestStatus.WaitingForManager;
        var isHrd = lr.Status == LeaveRequestStatus.WaitingForHR;

        if (!isManager && !isHrd)
            throw new InvalidOperationException("Leave request is not awaiting approval");

        // Check if approver is the employee's manager
        if (isManager)
        {
            var employee = await _context.Employees.Include(e => e.Manager).FirstOrDefaultAsync(e => e.Id == lr.EmployeeId);
            if (employee?.Manager?.UserId != approverId)
                throw new UnauthorizedAccessException("Only the employee's manager can approve at this stage");
        }

        var approval = new LeaveApproval
        {
            LeaveRequestId = id,
            ApproverId = approverId,
            ApproverRole = isManager ? "Manager" : "HRD",
            Status = ApprovalStatus.Approved,
            Note = null,
            ApprovedAt = DateTime.UtcNow
        };
        _context.LeaveApprovals.Add(approval);

        // Multi-level logic
        if (isManager)
        {
            // ≤ 3 days: Manager only → Approved
            // > 3 days: Manager → WaitingForHR
            if (lr.TotalDays <= 3 || lr.LeaveType?.SkipManagerApproval == true)
            {
                lr.Status = LeaveRequestStatus.Approved;
                await FinalizeApprovalAsync(lr);
            }
            else
            {
                lr.Status = LeaveRequestStatus.WaitingForHR;
            }
        }
        else if (isHrd)
        {
            lr.Status = LeaveRequestStatus.Approved;
            await FinalizeApprovalAsync(lr);
        }

        lr.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Notify employee
        var emp = await _context.Employees.Include(e => e.Manager).FirstOrDefaultAsync(e => e.Id == lr.EmployeeId);
        if (emp?.UserId != null)
        {
            await _notificationService.CreateNotificationAsync(
                emp.UserId.Value,
                "Leave Request Approved",
                $"Your leave request ({lr.TotalDays} days) has been approved",
                "Success", "LeaveRequest", lr.Id);
        }

        return await GetLeaveRequestAsync(id);
    }

    public async Task<LeaveRequestResponse> RejectAsync(Guid id, Guid approverId, string reason)
    {
        var lr = await _context.LeaveRequests
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(lr => lr.Id == id);

        if (lr == null)
            throw new KeyNotFoundException("Leave request not found");

        if (lr.Status != LeaveRequestStatus.WaitingForManager && lr.Status != LeaveRequestStatus.WaitingForHR)
            throw new InvalidOperationException("Leave request is not awaiting approval");

        var approval = new LeaveApproval
        {
            LeaveRequestId = id,
            ApproverId = approverId,
            ApproverRole = lr.Status == LeaveRequestStatus.WaitingForManager ? "Manager" : "HRD",
            Status = ApprovalStatus.Rejected,
            Note = reason,
            ApprovedAt = DateTime.UtcNow
        };
        _context.LeaveApprovals.Add(approval);

        lr.Status = LeaveRequestStatus.Rejected;
        lr.RejectionReason = reason;
        lr.UpdatedAt = DateTime.UtcNow;

        // Release pending balance
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb =>
                lb.EmployeeId == lr.EmployeeId &&
                lb.LeaveTypeId == lr.LeaveTypeId &&
                lb.Year == DateTime.UtcNow.Year);

        if (balance != null)
        {
            balance.PendingDays -= lr.TotalDays;
        }

        await _context.SaveChangesAsync();

        // Notify employee
        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == lr.EmployeeId);
        if (emp?.UserId != null)
        {
            await _notificationService.CreateNotificationAsync(
                emp.UserId.Value,
                "Leave Request Rejected",
                $"Your leave request has been rejected. Reason: {reason}",
                "Error", "LeaveRequest", lr.Id);
        }

        return await GetLeaveRequestAsync(id);
    }

    public async Task<LeaveRequestResponse> CancelAsync(Guid id, Guid employeeId)
    {
        var lr = await _context.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.Id == id && lr.EmployeeId == employeeId);

        if (lr == null)
            throw new KeyNotFoundException("Leave request not found");

        if (lr.Status != LeaveRequestStatus.Draft && lr.Status != LeaveRequestStatus.Submitted &&
            lr.Status != LeaveRequestStatus.WaitingForManager && lr.Status != LeaveRequestStatus.WaitingForHR)
            throw new InvalidOperationException("This leave request cannot be cancelled");

        lr.Status = LeaveRequestStatus.Cancelled;
        lr.UpdatedAt = DateTime.UtcNow;

        // Release pending balance
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb =>
                lb.EmployeeId == employeeId &&
                lb.LeaveTypeId == lr.LeaveTypeId &&
                lb.Year == DateTime.UtcNow.Year);

        if (balance != null)
        {
            balance.PendingDays -= lr.TotalDays;
        }

        await _context.SaveChangesAsync();

        return await GetLeaveRequestAsync(id);
    }

    public async Task<LeaveRequestListResponse> GetPendingApprovalsAsync(Guid userId, int page, int pageSize)
    {
        // Get employees managed by this user
        var managedEmployeeIds = await _context.Employees
            .Where(e => e.ManagerId != null && e.Manager!.UserId == userId)
            .Select(e => e.Id)
            .ToListAsync();

        var query = _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.Approvals).ThenInclude(a => a.Approver)
            .Where(lr =>
                (lr.Status == LeaveRequestStatus.WaitingForManager && managedEmployeeIds.Contains(lr.EmployeeId)) ||
                (lr.Status == LeaveRequestStatus.WaitingForHR));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(lr => lr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(lr => MapToResponse(lr))
            .ToListAsync();

        return new LeaveRequestListResponse(items, totalCount, page, pageSize);
    }

    private async Task FinalizeApprovalAsync(LeaveRequest lr)
    {
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb =>
                lb.EmployeeId == lr.EmployeeId &&
                lb.LeaveTypeId == lr.LeaveTypeId &&
                lb.Year == DateTime.UtcNow.Year);

        if (balance != null)
        {
            balance.UsedDays += lr.TotalDays;
            balance.PendingDays -= lr.TotalDays;
        }
    }

    private static LeaveRequestResponse MapToResponse(LeaveRequest lr) => new(
        lr.Id, lr.EmployeeId, lr.Employee.FullName, lr.Employee.EmployeeNo,
        lr.LeaveTypeId, lr.LeaveType.Name,
        lr.StartDate, lr.EndDate, lr.TotalDays, lr.Reason,
        lr.Status.ToString(), lr.AttachmentUrl, lr.RejectionReason,
        lr.CreatedAt,
        lr.Approvals.Select(a => new LeaveApprovalDto(
            a.Id, a.ApproverId, a.Approver.FullName,
            a.ApproverRole, a.Status.ToString(),
            a.Note, a.ApprovedAt)).ToList());
}
