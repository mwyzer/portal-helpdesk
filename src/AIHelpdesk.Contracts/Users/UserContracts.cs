namespace AIHelpdesk.Contracts.Users;

public record CreateUserRequest(
    string Email,
    string Password,
    string FullName,
    string? NIK,
    Guid? DepartmentId,
    Guid? PositionId,
    IList<Guid>? RoleIds);

public record UpdateUserRequest(
    string FullName,
    string? NIK,
    Guid? DepartmentId,
    Guid? PositionId,
    bool IsActive);

public record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    string? NIK,
    string? Department,
    string? Position,
    bool IsActive,
    IList<string> Roles,
    DateTime CreatedAt);

public record UserDetailResponse(
    Guid Id,
    string Email,
    string FullName,
    string? NIK,
    Guid? DepartmentId,
    string? Department,
    Guid? PositionId,
    string? Position,
    bool IsActive,
    IList<RoleInfo> Roles,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record RoleInfo(Guid Id, string Name);
