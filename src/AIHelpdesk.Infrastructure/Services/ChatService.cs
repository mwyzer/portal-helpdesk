using System.Text.Json;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Chat;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;
    private readonly IAIService _ai;
    private readonly IKnowledgeBaseService _kb;

    private const string SystemPrompt = @"You are an AI Helpdesk assistant for an organization. 
Answer questions based ONLY on the provided context below. 
If the context does not contain the answer, say 'I cannot find information about this in the knowledge base. Would you like me to escalate this to a human agent?'
Be concise, professional, and cite sources when possible.
Do not make up information. Do not disclose personal data outside the provided context.

Context:
{0}";

    public ChatService(ApplicationDbContext context, IAIService ai, IKnowledgeBaseService kb)
    {
        _context = context;
        _ai = ai;
        _kb = kb;
    }

    public async Task<ChatSessionDetailResponse> SendMessageAsync(Guid userId, SendMessageRequest request, Action<string>? onToken = null, Action<AIResponseMetadata>? onComplete = null)
    {
        // Get or create session
        ChatSession session;
        if (request.SessionId.HasValue)
        {
            session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId.Value && s.UserId == userId)
                ?? throw new KeyNotFoundException("Session not found");
        }
        else
        {
            session = new ChatSession
            {
                UserId = userId,
                Title = request.Message.Length > 50 ? request.Message[..50] + "..." : request.Message
            };
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        // Save user message
        var userMessage = new ChatMessage
        {
            SessionId = session.Id,
            Role = ChatMessageRole.User,
            Content = request.Message
        };
        _context.ChatMessages.Add(userMessage);
        await _context.SaveChangesAsync();

        // RAG: search knowledge base
        var searchResults = await _kb.SearchAsync(request.Message, topK: 5);
        var contextText = string.Join("\n\n", searchResults.Select((r, i) => $"[Source {i + 1}: {r.DocumentTitle}]\n{r.Content}"));
        var sources = JsonSerializer.Serialize(searchResults.Select(r => new { r.DocumentId, r.DocumentTitle, r.ChunkId, r.Content, r.Relevance }));

        // Build history
        var history = session.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => (m.Role == ChatMessageRole.User ? "user" : "assistant", m.Content))
            .ToList();

        var systemPrompt = string.Format(SystemPrompt, string.IsNullOrEmpty(contextText) ? "No relevant documents found." : contextText);

        // Get AI response (streaming)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var responseContent = await _ai.GenerateChatResponseAsync(systemPrompt, request.Message, history, onToken);
        sw.Stop();

        // Save assistant message
        var assistantMessage = new ChatMessage
        {
            SessionId = session.Id,
            Role = ChatMessageRole.Assistant,
            Content = responseContent,
            Sources = sources
        };
        _context.ChatMessages.Add(assistantMessage);
        await _context.SaveChangesAsync();

        // Save AI response metadata
        var aiResponse = new AIResponse
        {
            MessageId = assistantMessage.Id,
            ModelUsed = "gpt-4o-mini",
            PromptTokens = _ai.EstimateTokenCount(systemPrompt + request.Message),
            CompletionTokens = _ai.EstimateTokenCount(responseContent),
            TotalTokens = _ai.EstimateTokenCount(systemPrompt + request.Message + responseContent),
            LatencyMs = sw.ElapsedMilliseconds
        };
        _context.AIResponses.Add(aiResponse);
        await _context.SaveChangesAsync();

        // Usage log
        _context.AIUsageLogs.Add(new AIUsageLog
        {
            UserId = userId,
            Endpoint = "chat/completions",
            TokensUsed = aiResponse.TotalTokens,
            Cost = aiResponse.TotalTokens * 0.00000015m // rough GPT-4o-mini pricing
        });
        await _context.SaveChangesAsync();

        onComplete?.Invoke(new AIResponseMetadata(
            aiResponse.Id,
            aiResponse.ModelUsed,
            aiResponse.PromptTokens,
            aiResponse.CompletionTokens,
            aiResponse.TotalTokens,
            aiResponse.LatencyMs,
            null, null));

        return await GetSessionAsync(session.Id, userId);
    }

    public async Task<ChatSessionDetailResponse> GetSessionAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
            ?? throw new KeyNotFoundException("Session not found");

        return MapDetail(session);
    }

    public async Task<PagedResult<ChatSessionResponse>> GetSessionsAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.ChatSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ChatSessionResponse(
                s.Id,
                s.Title,
                s.Status.ToString(),
                s.Messages.Count,
                s.CreatedAt,
                s.UpdatedAt))
            .ToListAsync();

        return new PagedResult<ChatSessionResponse>(items, total, page, pageSize);
    }

    public async Task DeleteSessionAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
            ?? throw new KeyNotFoundException("Session not found");
        session.IsDeleted = true;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<ChatSessionResponse> UpdateSessionAsync(Guid sessionId, Guid userId, UpdateSessionRequest request)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
            ?? throw new KeyNotFoundException("Session not found");
        session.Title = request.Title;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapSummary(session);
    }

    public async Task SubmitFeedbackAsync(Guid messageId, Guid userId, SubmitFeedbackRequest request)
    {
        var aiResponse = await _context.AIResponses
            .Include(r => r.Message).ThenInclude(m => m.Session)
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.Message.Session.UserId == userId)
            ?? throw new KeyNotFoundException("Message not found");
        aiResponse.FeedbackScore = request.Score;
        aiResponse.FeedbackComment = request.Comment;
        aiResponse.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<ChatSessionResponse> EscalateSessionAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId)
            ?? throw new KeyNotFoundException("Session not found");
        session.Status = ChatSessionStatus.Escalated;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapSummary(session);
    }

    private static ChatSessionResponse MapSummary(ChatSession s) => new(
        s.Id, s.Title, s.Status.ToString(), s.Messages.Count, s.CreatedAt, s.UpdatedAt);

    private static ChatSessionDetailResponse MapDetail(ChatSession s) => new(
        s.Id, s.Title, s.Status.ToString(),
        s.Messages.OrderBy(m => m.CreatedAt).Select(m => new ChatMessageResponse(
            m.Id, m.Role.ToString().ToLowerInvariant(), m.Content, m.Sources, m.CreatedAt)).ToList(),
        s.CreatedAt, s.UpdatedAt);
}
