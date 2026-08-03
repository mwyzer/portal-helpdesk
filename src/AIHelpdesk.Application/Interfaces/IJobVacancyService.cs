using AIHelpdesk.Contracts.Recruitment;

namespace AIHelpdesk.Application.Interfaces;

public interface IJobVacancyService
{
    Task<PagedResult<JobVacancyResponse>> GetAllAsync(int page, int pageSize, string? status, Guid? departmentId);
    Task<JobVacancyResponse> GetByIdAsync(Guid id);
    Task<JobVacancyResponse> CreateAsync(Guid userId, CreateJobVacancyRequest request);
    Task<JobVacancyResponse> UpdateAsync(Guid id, UpdateJobVacancyRequest request);
    Task<JobVacancyResponse> PublishAsync(Guid id);
    Task<JobVacancyResponse> CloseAsync(Guid id);
}
