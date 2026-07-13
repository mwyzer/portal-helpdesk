using AIHelpdesk.Contracts.Documents;

namespace AIHelpdesk.Application.Interfaces;

public interface IDocumentService
{
    // Templates
    Task<IList<DocumentTemplateResponse>> GetTemplatesAsync(string? category);
    Task<DocumentTemplateResponse> GetTemplateByIdAsync(Guid id);
    Task<DocumentTemplateResponse> CreateTemplateAsync(CreateDocumentTemplateRequest request);
    Task<DocumentTemplateResponse> UpdateTemplateAsync(Guid id, UpdateDocumentTemplateRequest request);
    Task DeleteTemplateAsync(Guid id);

    // Requests
    Task<PagedResult<DocumentRequestResponse>> GetDocumentRequestsAsync(Guid userId, int page, int pageSize, string? status);
    Task<DocumentRequestDetailResponse> GetDocumentRequestByIdAsync(Guid id);
    Task<DocumentRequestResponse> CreateDocumentRequestAsync(Guid employeeId, CreateDocumentRequestRequest request);
    Task<DocumentRequestResponse> UpdateDocumentRequestAsync(Guid id, UpdateDocumentRequestRequest request);
    Task<DocumentRequestResponse> GenerateDraftAsync(Guid id);
    Task<DocumentRequestResponse> SubmitForReviewAsync(Guid id);
    Task<DocumentRequestResponse> ApproveDocumentAsync(Guid id, Guid reviewerId);
    Task<DocumentRequestResponse> RejectDocumentAsync(Guid id, string reason);
    Task<DocumentRequestResponse> GenerateFinalAsync(Guid id);
    Task<(byte[] FileContents, string FileName, string ContentType)> DownloadDocumentAsync(Guid id);
}
