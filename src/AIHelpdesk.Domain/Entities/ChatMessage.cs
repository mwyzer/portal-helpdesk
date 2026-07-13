using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid SessionId { get; set; }
    public ChatMessageRole Role { get; set; } = ChatMessageRole.User;
    public string Content { get; set; } = string.Empty;
    public string? Sources { get; set; }

    public ChatSession Session { get; set; } = null!;
    public AIResponse? AIResponse { get; set; }
}
