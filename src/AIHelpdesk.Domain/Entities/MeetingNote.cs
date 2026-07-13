using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class MeetingNote : BaseEntity
{
    public Guid MeetingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsAISummary { get; set; }

    public Meeting Meeting { get; set; } = null!;
}
