using AIHelpdesk.Contracts.ActionItems;

namespace AIHelpdesk.Application.Interfaces;

public interface IActionItemService
{
    Task<PagedResult<ActionItemResponse>> GetMyActionItemsAsync(Guid userId, int page, int pageSize, string? status);
    Task<PagedResult<ActionItemResponse>> GetTeamActionItemsAsync(Guid managerId, int page, int pageSize);
    Task<IList<ActionItemResponse>> GetOverdueActionItemsAsync(Guid userId);
    Task<ActionItemResponse> GetActionItemByIdAsync(Guid id);
    Task<ActionItemResponse> CreateActionItemAsync(CreateActionItemRequest request);
    Task<ActionItemResponse> UpdateActionItemAsync(Guid id, UpdateActionItemRequest request);
    Task<ActionItemResponse> CompleteActionItemAsync(Guid id, Guid userId);
    Task<ActionItemResponse> CancelActionItemAsync(Guid id);
}
