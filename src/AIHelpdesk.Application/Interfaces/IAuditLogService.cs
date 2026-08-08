using AIHelpdesk.Contracts.AuditLogs;

namespace AIHelpdesk.Application.Interfaces;

public interface IAuditLogService
{
    Task<AuditLogListResponse> GetAuditLogsAsync(
        int page, int pageSize, string? entityName, Guid? userId, string? action, DateTime? from, DateTime? to);
}
