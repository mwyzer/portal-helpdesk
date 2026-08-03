using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class CandidateStageHistory : BaseEntity
{
    public Guid CandidateId { get; set; }
    public CandidateStage FromStage { get; set; }
    public CandidateStage ToStage { get; set; }
    public Guid ChangedById { get; set; }
    public string? Notes { get; set; }

    public Candidate Candidate { get; set; } = null!;
    public ApplicationUser ChangedBy { get; set; } = null!;
}
