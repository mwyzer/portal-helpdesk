using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class InterviewQuestion : BaseEntity
{
    public Guid InterviewId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsAIGenerated { get; set; }

    public Interview Interview { get; set; } = null!;
}
