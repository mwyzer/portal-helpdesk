using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class TicketAttachment : BaseEntity
{
    public Guid TicketId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public Guid UploadedById { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser UploadedBy { get; set; } = null!;
}
