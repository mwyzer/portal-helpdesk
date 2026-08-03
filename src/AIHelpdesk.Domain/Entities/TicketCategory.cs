using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class TicketCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority DefaultPriority { get; set; } = TicketPriority.Normal;
    public int SLAHours { get; set; } = 24;
    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
