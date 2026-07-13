using AIHelpdesk.Contracts.Knowledge;

namespace AIHelpdesk.Application.Interfaces;

public interface IKnowledgeBaseService
{
    Task<PagedResult<KnowledgeDocumentResponse>> GetDocumentsAsync(int page, int pageSize, string? status);
    Task<KnowledgeDocumentDetailResponse> GetDocumentAsync(Guid id);
    Task<KnowledgeDocumentResponse> UploadDocumentAsync(Guid userId, string title, string fileName, Stream fileStream, string contentType);
    Task DeleteDocumentAsync(Guid id);
    Task<KnowledgeDocumentResponse> IndexDocumentAsync(Guid id);
    Task<List<KnowledgeSearchResult>> SearchAsync(string query, int topK);
}
