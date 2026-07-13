using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class KnowledgeDocument : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public KnowledgeDocumentStatus Status { get; set; } = KnowledgeDocumentStatus.Pending;
    public int? PageCount { get; set; }
    public int? ChunkCount { get; set; }
    public string? ErrorMessage { get; set; }

    public ICollection<KnowledgeChunk> Chunks { get; set; } = new List<KnowledgeChunk>();
}
