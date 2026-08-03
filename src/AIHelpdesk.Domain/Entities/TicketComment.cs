using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class TicketComment : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
}
