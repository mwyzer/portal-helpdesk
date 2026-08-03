using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class TicketHistory : BaseEntity
{
    public Guid TicketId { get; set; }
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid ChangedById { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser ChangedBy { get; set; } = null!;
}
