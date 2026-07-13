using AIHelpdesk.Contracts.Users;

namespace AIHelpdesk.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserResponse>> GetUsersAsync(int page, int pageSize, string? search, bool? isActive);
    Task<UserDetailResponse> GetUserAsync(Guid id);
    Task<UserResponse> CreateUserAsync(CreateUserRequest request);
    Task<UserResponse> UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task DeleteUserAsync(Guid id);
    Task ActivateUserAsync(Guid id);
    Task DeactivateUserAsync(Guid id);
    Task<IList<RoleInfo>> GetUserRolesAsync(Guid id);
    Task AssignRolesAsync(Guid id, IList<Guid> roleIds);
}

public record PagedResult<T>(IList<T> Items, int TotalCount, int Page, int PageSize);
