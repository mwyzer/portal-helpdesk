using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class DocumentRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid TemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ContentDraft { get; set; }
    public string? ContentFinal { get; set; }
    public DocumentRequestStatus Status { get; set; } = DocumentRequestStatus.Draft;
    public string? LetterNumber { get; set; }
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }

    public ApplicationUser Employee { get; set; } = null!;
    public DocumentTemplate Template { get; set; } = null!;
    public ICollection<GeneratedDocument> GeneratedDocuments { get; set; } = new List<GeneratedDocument>();
}
