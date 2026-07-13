namespace AIHelpdesk.Domain.Common;

public enum MeetingStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

public enum ParticipantRole
{
    Organizer,
    Presenter,
    Attendee
}

public enum AttendanceStatus
{
    Pending,
    Accepted,
    Declined,
    Attended,
    Absent
}

public enum ActionItemStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled
}

public enum ActionItemPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public enum DocumentRequestStatus
{
    Draft,
    Submitted,
    AiDraftReady,
    Review,
    Approved,
    Rejected,
    Generated
}

public enum DocumentFormat
{
    PDF,
    DOCX
}
