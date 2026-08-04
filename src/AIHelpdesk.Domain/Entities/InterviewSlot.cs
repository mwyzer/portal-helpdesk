using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

/// <summary>
/// An open interview time offered by staff that a candidate can book via the self-service
/// portal. Booking converts it into a real <see cref="Interview"/> row (see
/// CandidatePortalService.BookSlotAsync) rather than the portal creating Interview rows directly.
/// </summary>
public class InterviewSlot : BaseEntity
{
    public Guid InterviewerId { get; set; }
    public Guid JobVacancyId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public InterviewType Type { get; set; } = InterviewType.Video;
    public InterviewSlotStatus Status { get; set; } = InterviewSlotStatus.Open;
    public Guid? BookedByCandidateId { get; set; }
    public Guid? InterviewId { get; set; }

    public ApplicationUser Interviewer { get; set; } = null!;
    public JobVacancy JobVacancy { get; set; } = null!;
    public Candidate? BookedByCandidate { get; set; }
    public Interview? Interview { get; set; }
}
