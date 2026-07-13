namespace AIHelpdesk.Contracts.Documents;

public record CreateDocumentTemplateRequest(
    string Name,
    string Code,
    string Category,
    string ContentTemplate,
    string Variables);

public record UpdateDocumentTemplateRequest(
    string Name,
    string Code,
    string Category,
    string ContentTemplate,
    string Variables,
    bool IsActive);

public record DocumentTemplateResponse(
    Guid Id,
    string Name,
    string Code,
    string Category,
    string ContentTemplate,
    string Variables,
    bool IsActive,
    DateTime CreatedAt);

public record CreateDocumentRequestRequest(
    Guid TemplateId,
    string Title,
    string? Notes);

public record UpdateDocumentRequestRequest(
    string Title,
    string? ContentDraft,
    string? Notes);

public record DocumentRequestResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid TemplateId,
    string TemplateName,
    string Title,
    string? ContentDraft,
    string? ContentFinal,
    string Status,
    string? LetterNumber,
    string? Notes,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record DocumentRequestDetailResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid TemplateId,
    string TemplateName,
    string Title,
    string? ContentDraft,
    string? ContentFinal,
    string Status,
    string? LetterNumber,
    string? Notes,
    string? RejectionReason,
    List<GeneratedDocumentResponse> GeneratedDocuments,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record GeneratedDocumentResponse(
    Guid Id,
    string FileName,
    string FilePath,
    string FileFormat,
    int Version,
    DateTime GeneratedAt);
