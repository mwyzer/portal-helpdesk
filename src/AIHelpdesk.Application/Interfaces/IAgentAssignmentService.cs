using AIHelpdesk.Contracts.Tickets;

namespace AIHelpdesk.Application.Interfaces;

public interface IAgentAssignmentService
{
    Task<IList<AgentAssignmentResponse>> GetByDepartmentAsync(Guid departmentId);
    Task<IList<AgentAssignmentResponse>> GetAllAsync();
    Task<AgentAssignmentResponse> CreateAsync(CreateAgentAssignmentRequest request);
    Task<AgentAssignmentResponse> UpdateAsync(Guid id, UpdateAgentAssignmentRequest request);
    Task DeleteAsync(Guid id);
    Task<Guid?> GetNextAvailableAgentAsync(Guid departmentId);
}
