using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class Ticket : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string? SubCategory { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public Guid AssignedToId { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public Guid SubmittedById { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime? SLADeadline { get; set; }
    public SLAStatus SLAStatus { get; set; } = SLAStatus.OnTrack;
    public DateTime? EscalatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public TicketCategory Category { get; set; } = null!;
    public ApplicationUser AssignedTo { get; set; } = null!;
    public ApplicationUser? AssignedAgent { get; set; }
    public ApplicationUser SubmittedBy { get; set; } = null!;
    public Department? Department { get; set; }
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
    public ICollection<TicketSLA> SLARecords { get; set; } = new List<TicketSLA>();
    public ICollection<Escalation> Escalations { get; set; } = new List<Escalation>();
}
