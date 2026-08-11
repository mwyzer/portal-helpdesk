namespace AIHelpdesk.Application.Interfaces;

public interface IAIService
{
    Task<IReadOnlyList<double>> GenerateEmbeddingAsync(string text);
    Task<string> GenerateChatResponseAsync(string systemPrompt, string userMessage, List<(string Role, string Content)> history, Action<string>? onToken = null);
    int EstimateTokenCount(string text);

    /// <summary>The configured chat model (AI:ChatModel), for attributing responses to the
    /// provider actually in use instead of a hardcoded guess.</summary>
    string ChatModel { get; }
}
