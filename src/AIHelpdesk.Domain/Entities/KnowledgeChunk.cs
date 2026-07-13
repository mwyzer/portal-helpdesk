using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class KnowledgeChunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public string EmbeddingJson { get; set; } = "[]";

    public KnowledgeDocument Document { get; set; } = null!;
}
