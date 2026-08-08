using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _service;

    public TicketsController(ITicketService service)
    {
        _service = service;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Ticket-specific ownership checks in TicketService allow these roles through regardless of
    // submitter/assignee, matching the roles already granted broader ticket-management endpoints
    // elsewhere in this controller (queue, assign, resolve, close, etc.).
    private bool IsPrivileged() => User.IsInRole("Agent") || User.IsInRole("Manager") || User.IsInRole("Super Admin");

    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketResponse>>> GetMyTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null)
    {
        var result = await _service.GetMyTicketsAsync(GetUserId(), page, pageSize, status, priority);
        return Ok(result);
    }

    [HttpGet("assigned")]
    [Authorize(Roles = "Agent,Manager,Super Admin")]
    public async Task<ActionResult<PagedResult<TicketResponse>>> GetAssignedTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var result = await _service.GetAssignedTicketsAsync(GetUserId(), page, pageSize, status);
        return Ok(result);
    }

    [HttpGet("department/{departmentId}")]
    [Authorize(Roles = "Manager,Super Admin")]
    public async Task<ActionResult<PagedResult<TicketResponse>>> GetDepartmentTickets(
        Guid departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var result = await _service.GetDepartmentTicketsAsync(departmentId, page, pageSize, status);
        return Ok(result);
    }

    [HttpGet("queue")]
    [Authorize(Roles = "Agent,Manager,Super Admin")]
    public async Task<ActionResult<PagedResult<TicketQueueResponse>>> GetQueue(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null)
    {
        var result = await _service.GetQueueAsync(departmentId, page, pageSize, status, priority);
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Roles = "Agent,Manager,Super Admin")]
    public async Task<ActionResult<TicketStatsResponse>> GetStats(
        [FromQuery] Guid? departmentId = null)
    {
        var result = await _service.GetStatsAsync(null, departmentId);
        return Ok(result);
    }

    [HttpGet("sla-report")]
    [Authorize(Roles = "Manager,Super Admin")]
    public async Task<ActionResult<IList<TicketSLAReportResponse>>> GetSLAReport(
        [FromQuery] Guid? departmentId = null)
    {
        var result = await _service.GetSLAReportAsync(departmentId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDetailResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id, GetUserId(), IsPrivileged());
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TicketDetailResponse>> Create([FromBody] CreateTicketRequest request)
    {
        var result = await _service.CreateAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TicketDetailResponse>> Update(Guid id, [FromBody] UpdateTicketRequest request)
    {
        var result = await _service.UpdateAsync(id, GetUserId(), IsPrivileged(), request);
        return Ok(result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Agent,Manager,Super Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
    {
        await _service.UpdateStatusAsync(id, status);
        return NoContent();
    }

    [HttpPost("{id}/assign")]
    [Authorize(Roles = "Manager,Super Admin")]
    public async Task<ActionResult<TicketDetailResponse>> AssignAgent(Guid id, [FromBody] Guid agentId)
    {
        var result = await _service.AssignAgentAsync(id, agentId);
        return Ok(result);
    }

    [HttpPost("{id}/comment")]
    public async Task<ActionResult<TicketDetailResponse>> AddComment(Guid id, [FromBody] CreateTicketCommentRequest request)
    {
        var result = await _service.AddCommentAsync(id, GetUserId(), IsPrivileged(), request);
        return Ok(result);
    }

    [HttpPost("{id}/resolve")]
    [Authorize(Roles = "Agent,Manager,Super Admin")]
    public async Task<ActionResult<TicketDetailResponse>> Resolve(Guid id)
    {
        var result = await _service.ResolveAsync(id, GetUserId());
        return Ok(result);
    }

    [HttpPost("{id}/close")]
    [Authorize(Roles = "Agent,Manager,Super Admin")]
    public async Task<ActionResult<TicketDetailResponse>> Close(Guid id)
    {
        var result = await _service.CloseAsync(id, GetUserId());
        return Ok(result);
    }

    [HttpPost("{id}/reopen")]
    public async Task<ActionResult<TicketDetailResponse>> Reopen(Guid id)
    {
        var result = await _service.ReopenAsync(id, GetUserId(), IsPrivileged());
        return Ok(result);
    }

    [HttpPost("{id}/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<ActionResult<TicketAttachmentResponse>> UploadAttachment(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        await using var stream = file.OpenReadStream();
        var result = await _service.UploadAttachmentAsync(id, GetUserId(), IsPrivileged(), file.FileName, file.ContentType, stream);
        return Ok(result);
    }

    [HttpGet("{id}/attachments/{attachmentId}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId)
    {
        var (stream, contentType, fileName) = await _service.DownloadAttachmentAsync(id, attachmentId, GetUserId(), IsPrivileged());
        return File(stream, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType, fileName);
    }

    [HttpDelete("{id}/attachments/{attachmentId}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
    {
        await _service.DeleteAttachmentAsync(id, attachmentId, GetUserId(), IsPrivileged());
        return NoContent();
    }

    [HttpGet("export")]
    [Authorize(Roles = "Agent,Manager,Super Admin")]
    public async Task<IActionResult> Export([FromQuery] Guid? departmentId, [FromQuery] string? status, [FromQuery] string? priority)
    {
        var data = await _service.ExportToExcelAsync(departmentId, status, priority);
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "tickets-export.xlsx");
    }

    [HttpPost("ai-suggestion")]
    public async Task<ActionResult<TicketAISuggestionResponse>> GetAISuggestion([FromBody] CreateTicketRequest request)
    {
        var result = await _service.GetAISuggestionAsync(request);
        return Ok(result);
    }
}
