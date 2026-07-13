using AIHelpdesk.Contracts.LeaveRequests;

namespace AIHelpdesk.Application.Interfaces;

public interface ILeaveRequestService
{
    Task<LeaveRequestListResponse> GetLeaveRequestsAsync(Guid userId, int page, int pageSize, string? status);
    Task<LeaveRequestResponse> GetLeaveRequestAsync(Guid id);
    Task<LeaveRequestResponse> CreateDraftAsync(Guid employeeId, CreateLeaveRequest request);
    Task<LeaveRequestResponse> UpdateDraftAsync(Guid id, Guid employeeId, UpdateLeaveRequest request);
    Task<LeaveRequestResponse> SubmitAsync(Guid id, Guid employeeId);
    Task<LeaveRequestResponse> ApproveAsync(Guid id, Guid approverId);
    Task<LeaveRequestResponse> RejectAsync(Guid id, Guid approverId, string reason);
    Task<LeaveRequestResponse> CancelAsync(Guid id, Guid employeeId);
    Task<LeaveRequestListResponse> GetPendingApprovalsAsync(Guid userId, int page, int pageSize);
}
