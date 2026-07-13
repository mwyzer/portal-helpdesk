using AIHelpdesk.Contracts.Roles;

namespace AIHelpdesk.Application.Interfaces;

public interface IRoleService
{
    Task<IList<RoleResponse>> GetRolesAsync();
    Task<RoleDetailResponse> GetRoleAsync(Guid id);
    Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request);
    Task<RoleResponse> UpdateRoleAsync(Guid id, UpdateRoleRequest request);
    Task DeleteRoleAsync(Guid id);
    Task<RoleDetailResponse> GetRolePermissionsAsync(Guid id);
    Task UpdateRolePermissionsAsync(Guid id, UpdateRolePermissionsRequest request);
    Task<IList<PermissionInfo>> GetAllPermissionsAsync();
}
