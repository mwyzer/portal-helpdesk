namespace AIHelpdesk.Domain.Common;

public enum EmploymentStatus
{
    Active,
    Inactive,
    Resigned,
    Terminated
}

public enum LeaveRequestStatus
{
    Draft,
    Submitted,
    WaitingForManager,
    WaitingForHR,
    Approved,
    Rejected,
    Cancelled
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
