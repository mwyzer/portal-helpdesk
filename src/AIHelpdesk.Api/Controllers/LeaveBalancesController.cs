using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.LeaveBalances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/leave-balances")]
[Authorize]
public class LeaveBalancesController : ControllerBase
{
    private readonly ILeaveBalanceService _leaveBalanceService;

    public LeaveBalancesController(ILeaveBalanceService leaveBalanceService)
    {
        _leaveBalanceService = leaveBalanceService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("my")]
    public async Task<ActionResult<IList<LeaveBalanceResponse>>> GetMyBalances()
    {
        var result = await _leaveBalanceService.GetMyBalancesAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("employee/{employeeId}")]
    [Authorize(Roles = "Super Admin,HRD,Manager")]
    public async Task<ActionResult<IList<LeaveBalanceResponse>>> GetEmployeeBalances(Guid employeeId)
    {
        var result = await _leaveBalanceService.GetEmployeeBalancesAsync(employeeId);
        return Ok(result);
    }

    [HttpPost("adjust")]
    [Authorize(Roles = "Super Admin,HRD")]
    public async Task<ActionResult> AdjustBalance([FromBody] AdjustLeaveBalanceRequest request)
    {
        await _leaveBalanceService.AdjustBalanceAsync(request);
        return NoContent();
    }
}
