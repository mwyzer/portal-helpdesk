using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class Meeting : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public Guid OrganizerId { get; set; }
    public string? Location { get; set; }
    public string? MeetingLink { get; set; }
    public string? Description { get; set; }
    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;
    public string? Notes { get; set; }
    public string? TranscriptUrl { get; set; }

    public ApplicationUser Organizer { get; set; } = null!;
    public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();
    public ICollection<MeetingNote> MeetingNotes { get; set; } = new List<MeetingNote>();
    public ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
}
