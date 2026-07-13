using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Super Admin")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<IList<RoleResponse>>> GetRoles()
    {
        var result = await _roleService.GetRolesAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoleDetailResponse>> GetRole(Guid id)
    {
        var result = await _roleService.GetRoleAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RoleResponse>> CreateRole([FromBody] CreateRoleRequest request)
    {
        var result = await _roleService.CreateRoleAsync(request);
        return CreatedAtAction(nameof(GetRole), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RoleResponse>> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var result = await _roleService.UpdateRoleAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRole(Guid id)
    {
        await _roleService.DeleteRoleAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<RoleDetailResponse>> GetRolePermissions(Guid id)
    {
        var result = await _roleService.GetRolePermissionsAsync(id);
        return Ok(result);
    }

    [HttpPut("{id}/permissions")]
    public async Task<ActionResult> UpdateRolePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest request)
    {
        await _roleService.UpdateRolePermissionsAsync(id, request);
        return NoContent();
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<IList<PermissionInfo>>> GetAllPermissions()
    {
        var result = await _roleService.GetAllPermissionsAsync();
        return Ok(result);
    }
}
