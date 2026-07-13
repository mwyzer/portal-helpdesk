using AIHelpdesk.Contracts.LeaveTypes;

namespace AIHelpdesk.Application.Interfaces;

public interface ILeaveTypeService
{
    Task<IList<LeaveTypeResponse>> GetAllAsync();
    Task<LeaveTypeResponse> GetByIdAsync(Guid id);
    Task<LeaveTypeResponse> CreateAsync(CreateLeaveTypeRequest request);
    Task<LeaveTypeResponse> UpdateAsync(Guid id, UpdateLeaveTypeRequest request);
    Task DeleteAsync(Guid id);
}
