using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.ActionItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/action-items")]
[Authorize]
public class ActionItemsController : ControllerBase
{
    private readonly IActionItemService _actionItemService;

    public ActionItemsController(IActionItemService actionItemService)
    {
        _actionItemService = actionItemService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<ActionItemResponse>>> GetMyActionItems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var result = await _actionItemService.GetMyActionItemsAsync(GetUserId(), page, pageSize, status);
        return Ok(result);
    }

    [HttpGet("team")]
    [Authorize(Roles = "Manager,Super Admin")]
    public async Task<ActionResult<PagedResult<ActionItemResponse>>> GetTeamActionItems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _actionItemService.GetTeamActionItemsAsync(GetUserId(), page, pageSize);
        return Ok(result);
    }

    [HttpGet("overdue")]
    public async Task<ActionResult<IList<ActionItemResponse>>> GetOverdueActionItems()
    {
        var result = await _actionItemService.GetOverdueActionItemsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ActionItemResponse>> GetActionItem(Guid id)
    {
        var result = await _actionItemService.GetActionItemByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Secretary,Manager,Super Admin")]
    public async Task<ActionResult<ActionItemResponse>> CreateActionItem([FromBody] CreateActionItemRequest request)
    {
        var result = await _actionItemService.CreateActionItemAsync(request);
        return CreatedAtAction(nameof(GetActionItem), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Secretary,Manager,Super Admin")]
    public async Task<ActionResult<ActionItemResponse>> UpdateActionItem(Guid id, [FromBody] UpdateActionItemRequest request)
    {
        var result = await _actionItemService.UpdateActionItemAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<ActionResult<ActionItemResponse>> CompleteActionItem(Guid id)
    {
        var result = await _actionItemService.CompleteActionItemAsync(id, GetUserId());
        return Ok(result);
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Secretary,Manager,Super Admin")]
    public async Task<ActionResult<ActionItemResponse>> CancelActionItem(Guid id)
    {
        var result = await _actionItemService.CancelActionItemAsync(id);
        return Ok(result);
    }
}
