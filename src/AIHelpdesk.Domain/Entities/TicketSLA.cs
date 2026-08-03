using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class TicketSLA : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid CategoryId { get; set; }
    public int TargetHours { get; set; }
    public DateTime? BreachedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public TicketCategory Category { get; set; } = null!;
}
