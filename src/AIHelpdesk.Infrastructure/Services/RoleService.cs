using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Roles;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public RoleService(RoleManager<ApplicationRole> roleManager, ApplicationDbContext context)
    {
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<IList<RoleResponse>> GetRolesAsync()
    {
        var roles = await _context.Roles
            .Select(r => new RoleResponse(
                r.Id, r.Name!, r.Description, r.IsActive,
                _context.UserRoles.Count(ur => ur.RoleId == r.Id),
                r.CreatedAt))
            .ToListAsync();

        return roles;
    }

    public async Task<RoleDetailResponse> GetRoleAsync(Guid id)
    {
        var role = await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            throw new KeyNotFoundException("Role not found");

        return MapToDetail(role);
    }

    public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request)
    {
        var role = new ApplicationRole
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        if (request.PermissionIds?.Any() == true)
        {
            var permissions = await _context.Permissions.Where(p => request.PermissionIds.Contains(p.Id)).ToListAsync();
            role.Permissions = permissions;
            await _context.SaveChangesAsync();
        }

        return new RoleResponse(role.Id, role.Name!, role.Description, role.IsActive, 0, role.CreatedAt);
    }

    public async Task<RoleResponse> UpdateRoleAsync(Guid id, UpdateRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            throw new KeyNotFoundException("Role not found");

        role.Name = request.Name;
        role.Description = request.Description;
        role.IsActive = request.IsActive;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return new RoleResponse(role.Id, role.Name!, role.Description, role.IsActive,
            await _context.UserRoles.CountAsync(ur => ur.RoleId == role.Id), role.CreatedAt);
    }

    public async Task DeleteRoleAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            throw new KeyNotFoundException("Role not found");

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<RoleDetailResponse> GetRolePermissionsAsync(Guid id)
    {
        var role = await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            throw new KeyNotFoundException("Role not found");

        return MapToDetail(role);
    }

    public async Task UpdateRolePermissionsAsync(Guid id, UpdateRolePermissionsRequest request)
    {
        var role = await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            throw new KeyNotFoundException("Role not found");

        role.Permissions.Clear();
        var permissions = await _context.Permissions.Where(p => request.PermissionIds.Contains(p.Id)).ToListAsync();
        role.Permissions = permissions;

        await _context.SaveChangesAsync();
    }

    public async Task<IList<PermissionInfo>> GetAllPermissionsAsync()
    {
        var permissions = await _context.Permissions
            .OrderBy(p => p.Group)
            .ThenBy(p => p.Name)
            .Select(p => new PermissionInfo(p.Id, p.Name, p.Group, p.Description))
            .ToListAsync();

        return permissions;
    }

    private static RoleDetailResponse MapToDetail(ApplicationRole role)
    {
        return new RoleDetailResponse(
            role.Id, role.Name!, role.Description, role.IsActive,
            role.Permissions.Select(p => new PermissionInfo(p.Id, p.Name, p.Group, p.Description)).ToList(),
            role.CreatedAt, role.UpdatedAt);
    }
}
