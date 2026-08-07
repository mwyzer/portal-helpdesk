using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class KnowledgeChunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public string EmbeddingJson { get; set; } = "[]";

    /// <summary>
    /// Denormalized from Document.DepartmentId so a per-chunk partial/filtered vector index
    /// doesn't require a join. Kept in sync at index time; stale if the parent document's
    /// DepartmentId changes after indexing (re-index to refresh).
    /// </summary>
    public Guid? DepartmentId { get; set; }

    public KnowledgeDocument Document { get; set; } = null!;
}
