using AIHelpdesk.Contracts.Meetings;

namespace AIHelpdesk.Application.Interfaces;

public interface IMeetingService
{
    Task<PagedResult<MeetingResponse>> GetMeetingsAsync(int page, int pageSize, DateTime? from, DateTime? to, string? status);
    Task<MeetingDetailResponse> GetMeetingByIdAsync(Guid id);
    Task<MeetingResponse> CreateMeetingAsync(Guid organizerId, CreateMeetingRequest request);
    Task<MeetingResponse> UpdateMeetingAsync(Guid id, UpdateMeetingRequest request);
    Task DeleteMeetingAsync(Guid id);
    Task<MeetingParticipantResponse> AddParticipantAsync(Guid meetingId, AddParticipantRequest request);
    Task RemoveParticipantAsync(Guid meetingId, Guid participantId);
    Task<IList<MeetingResponse>> GetTodayMeetingsAsync(Guid userId);
    Task<IList<MeetingResponse>> GetUpcomingMeetingsAsync(Guid userId);
    Task<MeetingNoteResponse> AddNoteAsync(Guid meetingId, Guid userId, CreateMeetingNoteRequest request);
    Task<MeetingNoteResponse> UpdateNoteAsync(Guid meetingId, Guid noteId, UpdateMeetingNoteRequest request);
    Task DeleteNoteAsync(Guid meetingId, Guid noteId);
    Task<IList<MeetingNoteResponse>> GetNotesAsync(Guid meetingId);
}
