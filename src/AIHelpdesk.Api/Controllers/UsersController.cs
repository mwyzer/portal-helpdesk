using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Super Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _userService.GetUsersAsync(page, pageSize, search, isActive);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetailResponse>> GetUser(Guid id)
    {
        var result = await _userService.GetUserAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var result = await _userService.UpdateUserAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult> ActivateUser(Guid id)
    {
        await _userService.ActivateUserAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult> DeactivateUser(Guid id)
    {
        await _userService.DeactivateUserAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/roles")]
    public async Task<ActionResult<IList<RoleInfo>>> GetUserRoles(Guid id)
    {
        var result = await _userService.GetUserRolesAsync(id);
        return Ok(result);
    }

    [HttpPut("{id}/roles")]
    public async Task<ActionResult> AssignRoles(Guid id, [FromBody] IList<Guid> roleIds)
    {
        await _userService.AssignRolesAsync(id, roleIds);
        return NoContent();
    }
}
