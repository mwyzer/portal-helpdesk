using AIHelpdesk.Contracts.Recruitment;

namespace AIHelpdesk.Application.Interfaces;

public interface IInterviewService
{
    Task<PagedResult<InterviewResponse>> GetAllAsync(int page, int pageSize, DateTime? fromDate, DateTime? toDate, Guid? candidateId);
    Task<InterviewResponse> GetByIdAsync(Guid id);
    Task<InterviewResponse> CreateAsync(CreateInterviewRequest request);
    Task<InterviewResponse> UpdateAsync(Guid id, UpdateInterviewRequest request);
    Task<InterviewResponse> CompleteAsync(Guid id, CompleteInterviewRequest request);
    Task<InterviewResponse> CancelAsync(Guid id);
    Task<IList<InterviewResponse>> GetUpcomingAsync(Guid? interviewerId);

    // ── Interview slots (staff opens them; candidates book them via CandidatePortalService) ──
    Task<InterviewSlotResponse> CreateSlotAsync(CreateInterviewSlotRequest request);
    Task<IList<InterviewSlotResponse>> GetSlotsAsync(Guid? jobVacancyId, Guid? interviewerId, string? status);
    Task CancelSlotAsync(Guid slotId);

    /// <summary>
    /// Converts an open slot into a real Interview for the given candidate: checks the slot is
    /// still Open and reuses the same interviewer double-booking check as CreateAsync. Throws
    /// InvalidOperationException if the slot is no longer open or conflicts. Not concurrency-safe
    /// against two simultaneous bookings of the same slot -- same read-then-write pattern as the
    /// rest of this codebase, not a transactional guarantee.
    /// </summary>
    Task<InterviewResponse> BookSlotAsync(Guid slotId, Guid candidateId);
}
