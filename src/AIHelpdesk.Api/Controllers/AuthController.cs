using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ip);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshTokenAsync(request, ip);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] string refreshToken)
    {
        await _authService.LogoutAsync(refreshToken);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(new { message = "Password reset successfully" });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> GetProfile()
    {
        var userId = GetUserId();
        var result = await _authService.GetProfileAsync(userId);
        return Ok(result);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        var result = await _authService.UpdateProfileAsync(userId, request);
        return Ok(result);
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();
        await _authService.ChangePasswordAsync(userId, request);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}
