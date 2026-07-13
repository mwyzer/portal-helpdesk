namespace AIHelpdesk.Contracts.Roles;

public record CreateRoleRequest(string Name, string? Description, IList<Guid>? PermissionIds);

public record UpdateRoleRequest(string Name, string? Description, bool IsActive);

public record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int UserCount,
    DateTime CreatedAt);

public record RoleDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    IList<PermissionInfo> Permissions,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record PermissionInfo(Guid Id, string Name, string Group, string? Description);

public record UpdateRolePermissionsRequest(IList<Guid> PermissionIds);
