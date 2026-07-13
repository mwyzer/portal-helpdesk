namespace AIHelpdesk.Contracts.Knowledge;

// Requests
public record UploadDocumentRequest(
    string Title);

public record SearchKnowledgeRequest(
    string Query,
    int TopK = 5);

// Responses
public record KnowledgeDocumentResponse(
    Guid Id,
    string Title,
    string FileName,
    string FileType,
    long FileSize,
    string Status,
    int? PageCount,
    int? ChunkCount,
    string? ErrorMessage,
    DateTime CreatedAt);

public record KnowledgeSearchResult(
    Guid DocumentId,
    string DocumentTitle,
    Guid ChunkId,
    string Content,
    double Relevance);

public record KnowledgeDocumentDetailResponse(
    Guid Id,
    string Title,
    string FileName,
    string FileType,
    string ContentType,
    long FileSize,
    string Status,
    int? PageCount,
    int? ChunkCount,
    string? ErrorMessage,
    List<KnowledgeSearchResult> SampleChunks,
    DateTime CreatedAt,
    DateTime UpdatedAt);
