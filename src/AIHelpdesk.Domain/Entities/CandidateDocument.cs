using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class CandidateDocument : BaseEntity
{
    public Guid CandidateId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    /// <summary>
    /// Null means the candidate uploaded this themselves via the self-service portal —
    /// CandidateId already identifies who, so no separate "uploaded by candidate" FK is needed.
    /// </summary>
    public Guid? UploadedById { get; set; }

    public Candidate Candidate { get; set; } = null!;
    public ApplicationUser? UploadedBy { get; set; }
}
