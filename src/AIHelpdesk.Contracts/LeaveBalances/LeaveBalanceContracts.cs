namespace AIHelpdesk.Contracts.LeaveBalances;

public record LeaveBalanceResponse(
    Guid Id,
    Guid EmployeeId,
    string? EmployeeName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    int Year,
    decimal TotalDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal RemainingDays
);

public record AdjustLeaveBalanceRequest(
    Guid EmployeeId,
    Guid LeaveTypeId,
    int Year,
    decimal AdjustmentDays,
    string Reason
);
