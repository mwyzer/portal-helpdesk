namespace AIHelpdesk.Contracts.Chat;

// Requests
public record SendMessageRequest(
    Guid? SessionId,
    string Message);

public record UpdateSessionRequest(
    string Title);

public record SubmitFeedbackRequest(
    int Score,
    string? Comment);

// Responses
public record ChatSessionResponse(
    Guid Id,
    string Title,
    string Status,
    int MessageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ChatMessageResponse(
    Guid Id,
    string Role,
    string Content,
    string? Sources,
    DateTime CreatedAt);

public record ChatSessionDetailResponse(
    Guid Id,
    string Title,
    string Status,
    List<ChatMessageResponse> Messages,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record AIResponseMetadata(
    Guid Id,
    string ModelUsed,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    long LatencyMs,
    int? FeedbackScore,
    string? FeedbackComment);

public record ChatStreamResponse(
    string Content,
    string? Sources,
    AIResponseMetadata? Metadata);
