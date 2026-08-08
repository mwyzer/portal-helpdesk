using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.AuditLogs;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLogListResponse> GetAuditLogsAsync(
        int page, int pageSize, string? entityName, Guid? userId, string? action, DateTime? from, DateTime? to)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(action) && Enum.TryParse<AuditAction>(action, true, out var parsedAction))
            query = query.Where(a => a.Action == parsedAction);

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogResponse(
                a.Id, a.Timestamp, a.UserId, a.UserName, a.Action.ToString(),
                a.EntityName, a.EntityId, a.Changes, a.IpAddress))
            .ToListAsync();

        return new AuditLogListResponse(items, totalCount, page, pageSize);
    }
}
