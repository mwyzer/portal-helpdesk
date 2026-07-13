using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Users;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UserService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<PagedResult<UserResponse>> GetUsersAsync(int page, int pageSize, string? search, bool? isActive)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var total = await query.CountAsync();

        var users = await query
            .Include(u => u.Department)
            .Include(u => u.Position)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<UserResponse>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserResponse(
                user.Id, user.Email ?? "", user.FullName, user.NIK,
                user.Department?.Name, user.Position?.Name,
                user.IsActive, roles.ToList(), user.CreatedAt));
        }

        return new PagedResult<UserResponse>(items, total, page, pageSize);
    }

    public async Task<UserDetailResponse> GetUserAsync(Guid id)
    {
        var user = await _userManager.Users
            .Include(u => u.Department)
            .Include(u => u.Position)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        var roles = await _userManager.GetRolesAsync(user);
        var roleEntities = await _context.Roles
            .Where(r => roles.Contains(r.Name!))
            .ToListAsync();

        return new UserDetailResponse(
            user.Id, user.Email ?? "", user.FullName, user.NIK,
            user.DepartmentId, user.Department?.Name,
            user.PositionId, user.Position?.Name,
            user.IsActive,
            roleEntities.Select(r => new RoleInfo(r.Id, r.Name!)).ToList(),
            user.CreatedAt, user.UpdatedAt);
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FullName = request.FullName,
            NIK = request.NIK,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        if (request.RoleIds?.Any() == true)
        {
            var roles = await _context.Roles.Where(r => request.RoleIds.Contains(r.Id)).ToListAsync();
            foreach (var role in roles)
                await _userManager.AddToRoleAsync(user, role.Name!);
        }

        return await GetUserResponseAsync(user);
    }

    public async Task<UserResponse> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            throw new KeyNotFoundException("User not found");

        user.FullName = request.FullName;
        user.NIK = request.NIK;
        user.DepartmentId = request.DepartmentId;
        user.PositionId = request.PositionId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return await GetUserResponseAsync(user);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            throw new KeyNotFoundException("User not found");

        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    public async Task ActivateUserAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            throw new KeyNotFoundException("User not found");

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    public async Task DeactivateUserAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            throw new KeyNotFoundException("User not found");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    public async Task<IList<RoleInfo>> GetUserRolesAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var roles = await _userManager.GetRolesAsync(user);
        return roles.Select(r => new RoleInfo(Guid.Empty, r)).ToList();
    }

    public async Task AssignRolesAsync(Guid id, IList<Guid> roleIds)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        var roles = await _context.Roles.Where(r => roleIds.Contains(r.Id)).ToListAsync();
        foreach (var role in roles)
            await _userManager.AddToRoleAsync(user, role.Name!);
    }

    private async Task<UserResponse> GetUserResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var dept = user.DepartmentId.HasValue ? await _context.Departments.FindAsync(user.DepartmentId.Value) : null;
        var pos = user.PositionId.HasValue ? await _context.Positions.FindAsync(user.PositionId.Value) : null;

        return new UserResponse(
            user.Id, user.Email ?? "", user.FullName, user.NIK,
            dept?.Name, pos?.Name,
            user.IsActive, roles.ToList(), user.CreatedAt);
    }
}
