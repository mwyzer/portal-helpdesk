using System.Security.Claims;
using System.Text.Json;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Chat;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ApplicationDbContext _context;

    public AIChatController(IChatService chatService, ApplicationDbContext context)
    {
        _chatService = chatService;
        _context = context;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health()
    {
        // Verify AI API key is configured
        var apiKey = _context.Database.GetConnectionString();
        return Ok(new
        {
            Status = "healthy",
            Database = !string.IsNullOrEmpty(apiKey),
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatSessionDetailResponse>> SendMessage([FromBody] SendMessageRequest request)
    {
        var result = await _chatService.SendMessageAsync(GetUserId(), request);
        return Ok(result);
    }

    [HttpPost("chat/stream")]
    public async Task SendMessageStream([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            var result = await _chatService.SendMessageAsync(
                GetUserId(),
                request,
                onToken: async token =>
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    var sseData = JsonSerializer.Serialize(new { type = "token", content = token });
                    await Response.WriteAsync($"data: {sseData}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                },
                onComplete: async metadata =>
                {
                    var sseMeta = JsonSerializer.Serialize(new
                    {
                        type = "metadata",
                        metadata = new
                        {
                            id = metadata.Id,
                            modelUsed = metadata.ModelUsed,
                            promptTokens = metadata.PromptTokens,
                            completionTokens = metadata.CompletionTokens,
                            totalTokens = metadata.TotalTokens,
                            latencyMs = metadata.LatencyMs
                        }
                    });
                    await Response.WriteAsync($"data: {sseMeta}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                });

            // Send final result with session info
            var finalData = JsonSerializer.Serialize(new
            {
                type = "complete",
                session = new
                {
                    result.Id,
                    result.Title,
                    result.Status,
                    messageCount = result.Messages.Count,
                    lastMessage = result.Messages.LastOrDefault(),
                    result.CreatedAt,
                    result.UpdatedAt
                }
            });
            await Response.WriteAsync($"data: {finalData}\n\n", cancellationToken);
            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — gracefully end stream
            await Response.WriteAsync("data: [CANCELLED]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<PagedResult<ChatSessionResponse>>> GetSessions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _chatService.GetSessionsAsync(GetUserId(), page, pageSize);
        return Ok(result);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<ActionResult<ChatSessionDetailResponse>> GetSession(Guid id)
    {
        var result = await _chatService.GetSessionAsync(id, GetUserId());
        return Ok(result);
    }

    [HttpPut("conversations/{id:guid}")]
    public async Task<ActionResult<ChatSessionResponse>> UpdateSession(Guid id, [FromBody] UpdateSessionRequest request)
    {
        var result = await _chatService.UpdateSessionAsync(id, GetUserId(), request);
        return Ok(result);
    }

    [HttpDelete("conversations/{id:guid}")]
    public async Task<ActionResult> DeleteSession(Guid id)
    {
        await _chatService.DeleteSessionAsync(id, GetUserId());
        return NoContent();
    }

    [HttpPost("responses/{messageId:guid}/feedback")]
    public async Task<ActionResult> SubmitFeedback(Guid messageId, [FromBody] SubmitFeedbackRequest request)
    {
        await _chatService.SubmitFeedbackAsync(messageId, GetUserId(), request);
        return Ok();
    }

    [HttpPost("conversations/{sessionId:guid}/escalate")]
    public async Task<ActionResult<ChatSessionResponse>> EscalateSession(Guid sessionId)
    {
        var result = await _chatService.EscalateSessionAsync(sessionId, GetUserId());
        return Ok(result);
    }

    [HttpGet("usage")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult> GetUsageStats(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        to ??= DateTime.UtcNow;

        var stats = await _context.AIUsageLogs
            .Where(l => l.CreatedAt >= from && l.CreatedAt <= to)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalRequests = g.Count(),
                TotalTokens = g.Sum(l => l.TokensUsed),
                TotalCost = g.Sum(l => l.Cost),
                UniqueUsers = g.Select(l => l.UserId).Distinct().Count(),
                From = from.Value,
                To = to.Value
            })
            .FirstOrDefaultAsync() ?? new
            {
                TotalRequests = 0,
                TotalTokens = 0,
                TotalCost = 0m,
                UniqueUsers = 0,
                From = from.Value,
                To = to.Value
            };

        var byDay = await _context.AIUsageLogs
            .Where(l => l.CreatedAt >= from && l.CreatedAt <= to)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Requests = g.Count(),
                Tokens = g.Sum(l => l.TokensUsed),
                Cost = g.Sum(l => l.Cost)
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        var byEndpoint = await _context.AIUsageLogs
            .Where(l => l.CreatedAt >= from && l.CreatedAt <= to)
            .GroupBy(l => l.Endpoint)
            .Select(g => new
            {
                Endpoint = g.Key,
                Requests = g.Count(),
                Tokens = g.Sum(l => l.TokensUsed),
                Cost = g.Sum(l => l.Cost)
            })
            .ToListAsync();

        var feedbackStats = await _context.AIResponses
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Positive = g.Count(r => r.FeedbackScore == 1),
                Negative = g.Count(r => r.FeedbackScore == 0),
                AvgLatencyMs = g.Average(r => r.LatencyMs)
            })
            .FirstOrDefaultAsync() ?? new { Total = 0, Positive = 0, Negative = 0, AvgLatencyMs = 0.0 };

        return Ok(new
        {
            Summary = stats,
            ByDay = byDay,
            ByEndpoint = byEndpoint,
            Feedback = feedbackStats
        });
    }
}
