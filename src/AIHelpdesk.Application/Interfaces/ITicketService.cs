using AIHelpdesk.Contracts.Tickets;

namespace AIHelpdesk.Application.Interfaces;

public interface ITicketService
{
    Task<PagedResult<TicketResponse>> GetMyTicketsAsync(Guid userId, int page, int pageSize, string? status, string? priority);
    Task<PagedResult<TicketResponse>> GetAssignedTicketsAsync(Guid agentId, int page, int pageSize, string? status);
    Task<PagedResult<TicketResponse>> GetDepartmentTicketsAsync(Guid departmentId, int page, int pageSize, string? status);
    Task<TicketDetailResponse> GetByIdAsync(Guid id, Guid userId, bool isPrivileged);
    Task<TicketDetailResponse> CreateAsync(Guid userId, CreateTicketRequest request);
    Task<TicketDetailResponse> UpdateAsync(Guid id, Guid userId, bool isPrivileged, UpdateTicketRequest request);
    Task UpdateStatusAsync(Guid id, string status);
    Task<TicketDetailResponse> AssignAgentAsync(Guid id, Guid agentId);
    Task<TicketDetailResponse> AddCommentAsync(Guid id, Guid userId, bool isPrivileged, CreateTicketCommentRequest request);
    Task<TicketDetailResponse> ResolveAsync(Guid id, Guid userId);
    Task<TicketDetailResponse> CloseAsync(Guid id, Guid userId);
    Task<TicketDetailResponse> ReopenAsync(Guid id, Guid userId, bool isPrivileged);
    Task<TicketAttachmentResponse> UploadAttachmentAsync(Guid ticketId, Guid userId, bool isPrivileged, string fileName, string contentType, Stream fileStream);
    Task<(Stream FileStream, string ContentType, string FileName)> DownloadAttachmentAsync(Guid ticketId, Guid attachmentId, Guid userId, bool isPrivileged);
    Task DeleteAttachmentAsync(Guid ticketId, Guid attachmentId, Guid userId, bool isPrivileged);
    Task<TicketStatsResponse> GetStatsAsync(Guid? userId, Guid? departmentId);
    Task<PagedResult<TicketQueueResponse>> GetQueueAsync(Guid? departmentId, int page, int pageSize, string? status, string? priority);
    Task<IList<TicketSLAReportResponse>> GetSLAReportAsync(Guid? departmentId);
    Task<TicketAISuggestionResponse> GetAISuggestionAsync(CreateTicketRequest request);
    Task<byte[]> ExportToExcelAsync(Guid? departmentId, string? status, string? priority);
}
