using AIHelpdesk.Contracts.ActionItems;

namespace AIHelpdesk.Contracts.Meetings;

public record CreateMeetingRequest(
    string Title,
    DateTime Date,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string? Location,
    string? MeetingLink,
    string? Description);

public record UpdateMeetingRequest(
    string Title,
    DateTime Date,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string? Location,
    string? MeetingLink,
    string? Description);

public record MeetingResponse(
    Guid Id,
    string Title,
    DateTime Date,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string? Location,
    string? MeetingLink,
    string? Description,
    string Status,
    string OrganizerName,
    int ParticipantCount,
    DateTime CreatedAt);

public record MeetingDetailResponse(
    Guid Id,
    string Title,
    DateTime Date,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string? Location,
    string? MeetingLink,
    string? Description,
    string Status,
    string? Notes,
    string? TranscriptUrl,
    Guid OrganizerId,
    string OrganizerName,
    List<MeetingParticipantResponse> Participants,
    List<MeetingNoteResponse> MeetingNotes,
    List<ActionItemResponse> ActionItems,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record MeetingParticipantResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string Role,
    bool IsRequired,
    string AttendanceStatus);

public record AddParticipantRequest(
    Guid EmployeeId,
    string Role,
    bool IsRequired);

public record CreateMeetingNoteRequest(
    string Title,
    string Content);

public record UpdateMeetingNoteRequest(
    string Title,
    string Content);

public record MeetingNoteResponse(
    Guid Id,
    string Title,
    string Content,
    bool IsAISummary,
    string CreatedByName,
    DateTime CreatedAt);
