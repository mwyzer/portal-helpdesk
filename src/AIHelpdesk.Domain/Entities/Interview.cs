using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class Interview : BaseEntity
{
    public Guid CandidateId { get; set; }
    public Guid InterviewerId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public InterviewType Type { get; set; } = InterviewType.Video;
    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
    public string? Feedback { get; set; }
    public int? Rating { get; set; }
    public InterviewRecommendation? Recommendation { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public Candidate Candidate { get; set; } = null!;
    public ApplicationUser Interviewer { get; set; } = null!;
    public ICollection<InterviewQuestion> Questions { get; set; } = new List<InterviewQuestion>();
}
