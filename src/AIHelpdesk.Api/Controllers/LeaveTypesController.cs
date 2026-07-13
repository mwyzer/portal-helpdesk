using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.LeaveTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/leave-types")]
[Authorize]
public class LeaveTypesController : ControllerBase
{
    private readonly ILeaveTypeService _leaveTypeService;

    public LeaveTypesController(ILeaveTypeService leaveTypeService)
    {
        _leaveTypeService = leaveTypeService;
    }

    [HttpGet]
    public async Task<ActionResult<IList<LeaveTypeResponse>>> GetAll()
    {
        var result = await _leaveTypeService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LeaveTypeResponse>> GetById(Guid id)
    {
        var result = await _leaveTypeService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Super Admin,HRD")]
    public async Task<ActionResult<LeaveTypeResponse>> Create([FromBody] CreateLeaveTypeRequest request)
    {
        var result = await _leaveTypeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Super Admin,HRD")]
    public async Task<ActionResult<LeaveTypeResponse>> Update(Guid id, [FromBody] UpdateLeaveTypeRequest request)
    {
        var result = await _leaveTypeService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _leaveTypeService.DeleteAsync(id);
        return NoContent();
    }
}
