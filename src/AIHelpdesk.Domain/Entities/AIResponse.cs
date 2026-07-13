using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class AIResponse : BaseEntity
{
    public Guid MessageId { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public long LatencyMs { get; set; }
    public int? FeedbackScore { get; set; }
    public string? FeedbackComment { get; set; }

    public ChatMessage Message { get; set; } = null!;
}
