using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.LeaveRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly IEmployeeService _employeeService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService, IEmployeeService employeeService)
    {
        _leaveRequestService = leaveRequestService;
        _employeeService = employeeService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid> GetEmployeeIdAsync()
    {
        try
        {
            var profile = await _employeeService.GetMyProfileAsync(GetUserId());
            return profile.Id;
        }
        catch
        {
            return Guid.Empty;
        }
    }

    [HttpGet]
    public async Task<ActionResult<LeaveRequestListResponse>> GetLeaveRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        var result = await _leaveRequestService.GetLeaveRequestsAsync(GetUserId(), page, pageSize, status);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LeaveRequestResponse>> GetLeaveRequest(Guid id)
    {
        var result = await _leaveRequestService.GetLeaveRequestAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestResponse>> CreateDraft([FromBody] CreateLeaveRequest request)
    {
        var employeeId = await GetEmployeeIdAsync();
        if (employeeId == Guid.Empty)
            return BadRequest("Employee profile not found. Please ensure your employee record exists.");

        var result = await _leaveRequestService.CreateDraftAsync(employeeId, request);
        return CreatedAtAction(nameof(GetLeaveRequest), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<LeaveRequestResponse>> UpdateDraft(Guid id, [FromBody] UpdateLeaveRequest request)
    {
        var employeeId = await GetEmployeeIdAsync();
        var result = await _leaveRequestService.UpdateDraftAsync(id, employeeId, request);
        return Ok(result);
    }

    [HttpPost("{id}/submit")]
    public async Task<ActionResult<LeaveRequestResponse>> Submit(Guid id)
    {
        var employeeId = await GetEmployeeIdAsync();
        var result = await _leaveRequestService.SubmitAsync(id, employeeId);
        return Ok(result);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Super Admin,HRD,Manager")]
    public async Task<ActionResult<LeaveRequestResponse>> Approve(Guid id)
    {
        var result = await _leaveRequestService.ApproveAsync(id, GetUserId());
        return Ok(result);
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Super Admin,HRD,Manager")]
    public async Task<ActionResult<LeaveRequestResponse>> Reject(Guid id, [FromBody] RejectRequest request)
    {
        var result = await _leaveRequestService.RejectAsync(id, GetUserId(), request.Reason);
        return Ok(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<LeaveRequestResponse>> Cancel(Guid id)
    {
        var employeeId = await GetEmployeeIdAsync();
        var result = await _leaveRequestService.CancelAsync(id, employeeId);
        return Ok(result);
    }

    [HttpGet("pending-approval")]
    [Authorize(Roles = "Super Admin,HRD,Manager")]
    public async Task<ActionResult<LeaveRequestListResponse>> GetPendingApprovals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _leaveRequestService.GetPendingApprovalsAsync(GetUserId(), page, pageSize);
        return Ok(result);
    }
}
