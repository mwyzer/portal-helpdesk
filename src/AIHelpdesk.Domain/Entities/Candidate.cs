using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class Candidate : BaseEntity
{
    public Guid JobVacancyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Source { get; set; }
    public CandidateStage Stage { get; set; } = CandidateStage.Applied;
    public string? AISummaryJson { get; set; }
    public string? RejectionReason { get; set; }

    public JobVacancy JobVacancy { get; set; } = null!;
    public ICollection<CandidateDocument> Documents { get; set; } = new List<CandidateDocument>();
    public ICollection<CandidateStageHistory> StageHistory { get; set; } = new List<CandidateStageHistory>();
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}
