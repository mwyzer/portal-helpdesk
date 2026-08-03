using AIHelpdesk.Contracts.Recruitment;

namespace AIHelpdesk.Application.Interfaces;

public interface ICandidateService
{
    Task<PagedResult<CandidateResponse>> GetAllAsync(int page, int pageSize, string? stage, Guid? jobVacancyId);
    Task<CandidateDetailResponse> GetByIdAsync(Guid id);
    Task<CandidateResponse> CreateAsync(CreateCandidateRequest request);
    Task<CandidateResponse> UpdateAsync(Guid id, UpdateCandidateRequest request);
    Task<CandidateDocumentResponse> UploadCvAsync(Guid candidateId, Guid userId, string fileName, string contentType, Stream fileStream);
    Task<(Stream FileStream, string ContentType, string FileName)> DownloadCvAsync(Guid candidateId, Guid documentId);
    Task<CandidateResponse> AdvanceStageAsync(Guid id, Guid userId, AdvanceCandidateStageRequest request);
    Task<CandidateResponse> RejectAsync(Guid id, Guid userId, RejectCandidateRequest request);
    Task<RecruitmentStatsResponse> GetStatsAsync();
    Task<byte[]> ExportToExcelAsync(Guid? jobVacancyId, string? stage);
}
