namespace AIHelpdesk.Contracts.LeaveRequests;

public record CreateLeaveRequest(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    string? AttachmentUrl
);

public record UpdateLeaveRequest(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    string? AttachmentUrl
);

public record RejectRequest(string Reason);

public record LeaveApprovalDto(
    Guid Id,
    Guid ApproverId,
    string ApproverName,
    string ApproverRole,
    string Status,
    string? Note,
    DateTime? ApprovedAt
);

public record LeaveRequestResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeNo,
    Guid LeaveTypeId,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason,
    string Status,
    string? AttachmentUrl,
    string? RejectionReason,
    DateTime CreatedAt,
    IList<LeaveApprovalDto> Approvals
);

public record LeaveRequestListResponse(
    IList<LeaveRequestResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);
