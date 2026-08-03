using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/agent-assignments")]
[Authorize]
public class AgentAssignmentsController : ControllerBase
{
    private readonly IAgentAssignmentService _service;

    public AgentAssignmentsController(IAgentAssignmentService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Manager,Super Admin")]
    public async Task<ActionResult<IList<AgentAssignmentResponse>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("department/{departmentId}")]
    [Authorize(Roles = "Manager,Super Admin")]
    public async Task<ActionResult<IList<AgentAssignmentResponse>>> GetByDepartment(Guid departmentId)
    {
        var result = await _service.GetByDepartmentAsync(departmentId);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<AgentAssignmentResponse>> Create([FromBody] CreateAgentAssignmentRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), new { result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<AgentAssignmentResponse>> Update(Guid id, [FromBody] UpdateAgentAssignmentRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
