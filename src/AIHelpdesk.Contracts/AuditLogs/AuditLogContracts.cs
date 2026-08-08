namespace AIHelpdesk.Contracts.AuditLogs;

public record AuditLogResponse(
    Guid Id,
    DateTime Timestamp,
    Guid? UserId,
    string? UserName,
    string Action,
    string EntityName,
    string EntityId,
    string Changes,
    string? IpAddress
);

public record AuditLogListResponse(
    IList<AuditLogResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);
