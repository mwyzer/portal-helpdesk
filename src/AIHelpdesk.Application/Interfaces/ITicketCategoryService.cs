using AIHelpdesk.Contracts.Tickets;

namespace AIHelpdesk.Application.Interfaces;

public interface ITicketCategoryService
{
    Task<IList<TicketCategoryResponse>> GetAllAsync(Guid? departmentId);
    Task<TicketCategoryResponse> GetByIdAsync(Guid id);
    Task<TicketCategoryResponse> CreateAsync(CreateTicketCategoryRequest request);
    Task<TicketCategoryResponse> UpdateAsync(Guid id, UpdateTicketCategoryRequest request);
    Task DeleteAsync(Guid id);
}
