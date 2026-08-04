using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class JobVacancy : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public int OpeningsCount { get; set; } = 1;
    public VacancyStatus Status { get; set; } = VacancyStatus.Draft;
    public Guid PostedById { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Department? Department { get; set; }
    public Position? Position { get; set; }
    public ApplicationUser PostedBy { get; set; } = null!;
    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
    public ICollection<InterviewSlot> InterviewSlots { get; set; } = new List<InterviewSlot>();
}
