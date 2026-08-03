using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/escalations")]
[Authorize]
public class EscalationsController : ControllerBase
{
    private readonly IEscalationService _service;

    public EscalationsController(IEscalationService service)
    {
        _service = service;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "Manager,Super Admin")]
    public async Task<ActionResult<PagedResult<EscalationResponse>>> GetEscalations(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var result = await _service.GetEscalationsAsync(departmentId, page, pageSize, status);
        return Ok(result);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Manager,Super Admin")]
    public async Task<ActionResult<IList<EscalationResponse>>> GetPending([FromQuery] Guid departmentId)
    {
        var result = await _service.GetPendingAsync(departmentId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<EscalationResponse>> Create([FromQuery] Guid ticketId, [FromBody] CreateEscalationRequest request)
    {
        var result = await _service.CreateAsync(ticketId, GetUserId(), request);
        return CreatedAtAction(nameof(GetEscalations), new { result.Id }, result);
    }

    [HttpPost("{id}/accept")]
    [Authorize(Roles = "Manager,Super Admin")]
    public async Task<IActionResult> Accept(Guid id)
    {
        await _service.AcceptAsync(id, GetUserId());
        return NoContent();
    }

    [HttpPost("{id}/resolve")]
    [Authorize(Roles = "Manager,Super Admin")]
    public async Task<IActionResult> Resolve(Guid id)
    {
        await _service.ResolveAsync(id, GetUserId());
        return NoContent();
    }

    [HttpPost("{id}/decline")]
    [Authorize(Roles = "Manager,Super Admin")]
    public async Task<IActionResult> Decline(Guid id)
    {
        await _service.DeclineAsync(id, GetUserId());
        return NoContent();
    }
}
