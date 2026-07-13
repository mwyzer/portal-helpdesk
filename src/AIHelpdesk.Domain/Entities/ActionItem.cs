using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class ActionItem : BaseEntity
{
    public Guid? MeetingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AssignedToId { get; set; }
    public DateTime DueDate { get; set; }
    public ActionItemPriority Priority { get; set; } = ActionItemPriority.Medium;
    public ActionItemStatus Status { get; set; } = ActionItemStatus.Open;
    public DateTime? CompletedAt { get; set; }

    public Meeting? Meeting { get; set; }
    public ApplicationUser AssignedTo { get; set; } = null!;
}
