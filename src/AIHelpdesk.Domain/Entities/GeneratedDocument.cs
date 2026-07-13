using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class GeneratedDocument : BaseEntity
{
    public Guid DocumentRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DocumentFormat FileFormat { get; set; } = DocumentFormat.PDF;
    public int Version { get; set; } = 1;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public DocumentRequest DocumentRequest { get; set; } = null!;
}
