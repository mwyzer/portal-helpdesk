using System.Security.Claims;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Recruitment;
using AIHelpdesk.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

/// <summary>
/// Candidate self-service portal. Every action except login/activate/refresh requires the
/// "CandidatePortal" auth scheme specifically (see Program.cs) -- a staff-issued token is
/// rejected here regardless of role, and this controller's token is rejected by every other
/// controller in turn. See TokenService.CandidatePortalScheme for why.
/// </summary>
[ApiController]
[Route("api/candidate-portal")]
public class CandidatePortalController : ControllerBase
{
    private readonly ICandidatePortalService _service;

    public CandidatePortalController(ICandidatePortalService service)
    {
        _service = service;
    }

    private Guid GetCandidateId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<CandidatePortalAuthResponse>> Login([FromBody] CandidatePortalLoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _service.LoginAsync(request, ip);
        return Ok(result);
    }

    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<ActionResult<CandidatePortalAuthResponse>> Activate([FromBody] CandidatePortalActivateRequest request)
    {
        var result = await _service.ActivateAccountAsync(request);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<CandidatePortalAuthResponse>> RefreshToken([FromBody] CandidatePortalRefreshRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _service.RefreshTokenAsync(request, ip);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = TokenService.CandidatePortalScheme)]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        await _service.LogoutAsync(refreshToken);
        return NoContent();
    }

    [HttpGet("status")]
    [Authorize(AuthenticationSchemes = TokenService.CandidatePortalScheme)]
    public async Task<ActionResult<CandidatePortalStatusResponse>> GetStatus()
    {
        var result = await _service.GetMyStatusAsync(GetCandidateId());
        return Ok(result);
    }

    [HttpGet("documents")]
    [Authorize(AuthenticationSchemes = TokenService.CandidatePortalScheme)]
    public async Task<ActionResult<IList<CandidateDocumentResponse>>> GetDocuments()
    {
        var result = await _service.GetMyDocumentsAsync(GetCandidateId());
        return Ok(result);
    }

    [HttpPost("documents")]
    [Authorize(AuthenticationSchemes = TokenService.CandidatePortalScheme)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<CandidatePortalUploadDocumentResponse>> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        await using var stream = file.OpenReadStream();
        var result = await _service.UploadMyDocumentAsync(GetCandidateId(), file.FileName, file.ContentType, stream);
        return Ok(result);
    }

    [HttpGet("interviews/available-slots")]
    [Authorize(AuthenticationSchemes = TokenService.CandidatePortalScheme)]
    public async Task<ActionResult<IList<AvailableInterviewSlotResponse>>> GetAvailableSlots()
    {
        var result = await _service.GetAvailableSlotsAsync(GetCandidateId());
        return Ok(result);
    }

    [HttpPost("interviews/slots/{slotId:guid}/book")]
    [Authorize(AuthenticationSchemes = TokenService.CandidatePortalScheme)]
    public async Task<ActionResult<CandidatePortalInterviewResponse>> BookSlot(Guid slotId)
    {
        var result = await _service.BookSlotAsync(GetCandidateId(), slotId);
        return Ok(result);
    }

    [HttpGet("interviews")]
    [Authorize(AuthenticationSchemes = TokenService.CandidatePortalScheme)]
    public async Task<ActionResult<IList<CandidatePortalInterviewResponse>>> GetInterviews()
    {
        var result = await _service.GetMyInterviewsAsync(GetCandidateId());
        return Ok(result);
    }
}
