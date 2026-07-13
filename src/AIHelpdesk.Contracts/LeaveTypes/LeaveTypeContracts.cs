namespace AIHelpdesk.Contracts.LeaveTypes;

public record CreateLeaveTypeRequest(
    string Name,
    string Code,
    int DaysPerYear,
    bool IsPaid,
    int MinServiceMonths,
    bool RequiresAttachment,
    bool SkipManagerApproval
);

public record UpdateLeaveTypeRequest(
    string Name,
    int DaysPerYear,
    bool IsPaid,
    bool IsActive,
    int MinServiceMonths,
    bool RequiresAttachment,
    bool SkipManagerApproval
);

public record LeaveTypeResponse(
    Guid Id,
    string Name,
    string Code,
    int DaysPerYear,
    bool IsPaid,
    bool IsActive,
    int MinServiceMonths,
    bool RequiresAttachment,
    bool SkipManagerApproval,
    DateTime CreatedAt
);
