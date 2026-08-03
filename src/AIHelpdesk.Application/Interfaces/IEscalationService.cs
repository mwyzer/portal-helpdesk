using AIHelpdesk.Contracts.Tickets;

namespace AIHelpdesk.Application.Interfaces;

public interface IEscalationService
{
    Task<PagedResult<EscalationResponse>> GetEscalationsAsync(Guid? departmentId, int page, int pageSize, string? status);
    Task<IList<EscalationResponse>> GetPendingAsync(Guid departmentId);
    Task<EscalationResponse> CreateAsync(Guid ticketId, Guid escalatedById, CreateEscalationRequest request);
    Task AcceptAsync(Guid id, Guid userId);
    Task ResolveAsync(Guid id, Guid userId);
    Task DeclineAsync(Guid id, Guid userId);
}
