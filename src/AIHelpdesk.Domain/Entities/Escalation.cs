using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class Escalation : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid EscalatedById { get; set; }
    public Guid? AssignedToId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public EscalationStatus Status { get; set; } = EscalationStatus.Pending;
    public DateTime? ResolvedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser EscalatedBy { get; set; } = null!;
    public ApplicationUser? AssignedTo { get; set; }
}
