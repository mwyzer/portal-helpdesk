using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Meetings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/meetings")]
[Authorize]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingService _meetingService;

    public MeetingsController(IMeetingService meetingService)
    {
        _meetingService = meetingService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<MeetingResponse>>> GetMeetings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? status = null)
    {
        var result = await _meetingService.GetMeetingsAsync(page, pageSize, from, to, status);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MeetingDetailResponse>> GetMeeting(Guid id)
    {
        var result = await _meetingService.GetMeetingByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Secretary,Manager,Super Admin")]
    public async Task<ActionResult<MeetingResponse>> CreateMeeting([FromBody] CreateMeetingRequest request)
    {
        var result = await _meetingService.CreateMeetingAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetMeeting), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Secretary,Manager,Super Admin")]
    public async Task<ActionResult<MeetingResponse>> UpdateMeeting(Guid id, [FromBody] UpdateMeetingRequest request)
    {
        var result = await _meetingService.UpdateMeetingAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Secretary,Super Admin")]
    public async Task<ActionResult> DeleteMeeting(Guid id)
    {
        await _meetingService.DeleteMeetingAsync(id);
        return NoContent();
    }

    [HttpGet("today")]
    public async Task<ActionResult<IList<MeetingResponse>>> GetTodayMeetings()
    {
        var result = await _meetingService.GetTodayMeetingsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<IList<MeetingResponse>>> GetUpcomingMeetings()
    {
        var result = await _meetingService.GetUpcomingMeetingsAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("{id}/participants")]
    [Authorize(Roles = "Secretary,Super Admin")]
    public async Task<ActionResult<MeetingParticipantResponse>> AddParticipant(Guid id, [FromBody] AddParticipantRequest request)
    {
        var result = await _meetingService.AddParticipantAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}/participants/{participantId}")]
    [Authorize(Roles = "Secretary,Super Admin")]
    public async Task<ActionResult> RemoveParticipant(Guid id, Guid participantId)
    {
        await _meetingService.RemoveParticipantAsync(id, participantId);
        return NoContent();
    }

    [HttpGet("{id}/notes")]
    public async Task<ActionResult<IList<MeetingNoteResponse>>> GetNotes(Guid id)
    {
        var result = await _meetingService.GetNotesAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/notes")]
    [Authorize(Roles = "Secretary,Manager,Super Admin")]
    public async Task<ActionResult<MeetingNoteResponse>> AddNote(Guid id, [FromBody] CreateMeetingNoteRequest request)
    {
        var result = await _meetingService.AddNoteAsync(id, GetUserId(), request);
        return Ok(result);
    }

    [HttpPut("{id}/notes/{noteId}")]
    [Authorize(Roles = "Secretary,Manager,Super Admin")]
    public async Task<ActionResult<MeetingNoteResponse>> UpdateNote(Guid id, Guid noteId, [FromBody] UpdateMeetingNoteRequest request)
    {
        var result = await _meetingService.UpdateNoteAsync(id, noteId, request);
        return Ok(result);
    }

    [HttpDelete("{id}/notes/{noteId}")]
    [Authorize(Roles = "Secretary,Manager,Super Admin")]
    public async Task<ActionResult> DeleteNote(Guid id, Guid noteId)
    {
        await _meetingService.DeleteNoteAsync(id, noteId);
        return NoContent();
    }
}
