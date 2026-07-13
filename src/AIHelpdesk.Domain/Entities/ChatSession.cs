using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class ChatSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = "New Chat";
    public ChatSessionStatus Status { get; set; } = ChatSessionStatus.Active;

    public ApplicationUser User { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
