using AIHelpdesk.Contracts.Chat;

namespace AIHelpdesk.Application.Interfaces;

public interface IChatService
{
    Task<ChatSessionDetailResponse> SendMessageAsync(Guid userId, SendMessageRequest request, Action<string>? onToken = null, Action<AIResponseMetadata>? onComplete = null);
    Task<ChatSessionDetailResponse> GetSessionAsync(Guid sessionId, Guid userId);
    Task<PagedResult<ChatSessionResponse>> GetSessionsAsync(Guid userId, int page, int pageSize);
    Task DeleteSessionAsync(Guid sessionId, Guid userId);
    Task<ChatSessionResponse> UpdateSessionAsync(Guid sessionId, Guid userId, UpdateSessionRequest request);
    Task SubmitFeedbackAsync(Guid messageId, Guid userId, SubmitFeedbackRequest request);
    Task<ChatSessionResponse> EscalateSessionAsync(Guid sessionId, Guid userId);
}
