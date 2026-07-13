using System.Security.Claims;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public AIChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("chat")]
    public async Task<ActionResult<ChatSessionDetailResponse>> SendMessage([FromBody] SendMessageRequest request)
    {
        var result = await _chatService.SendMessageAsync(GetUserId(), request);
        return Ok(result);
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
}
