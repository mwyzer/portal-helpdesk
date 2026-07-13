using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class AIUsageLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public decimal Cost { get; set; }
    public string? Metadata { get; set; }

    public ApplicationUser? User { get; set; }
}
