namespace AIHelpdesk.Domain.Common;

public enum TicketStatus
{
    Open,
    Assigned,
    InProgress,
    Resolved,
    Closed,
    Reopened,
    Rejected
}

public enum TicketPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum SLAStatus
{
    OnTrack,
    AtRisk,
    Breached
}

public enum EscalationStatus
{
    Pending,
    Accepted,
    Resolved,
    Declined
}
