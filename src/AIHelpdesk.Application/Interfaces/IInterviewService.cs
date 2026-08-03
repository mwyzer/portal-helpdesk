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
}
