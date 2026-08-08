using System.Security.Claims;
using System.Text.Json;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AIHelpdesk.Infrastructure.Data;

// Captures every business-entity mutation as an AuditLog row, without any individual service
// having to remember to log itself (todo-phase-7-hardening.md: "verify all data mutations are
// audit-logged" -- per-call-site logging was audited and found present in only 3 of ~40
// services, so the only reliable place to catch every mutation is here).
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    // Excluded: pure auth-token churn (every login/refresh would otherwise generate a row) and
    // the AuditLog table itself (avoid self-referential noise). Framework-managed Identity
    // tables (AspNetUserRoles, AspNetUserClaims, ...) are excluded implicitly below because they
    // live outside the AIHelpdesk.Domain.Entities namespace.
    private static readonly HashSet<Type> ExcludedTypes = [typeof(AuditLog), typeof(RefreshToken), typeof(CandidatePortalRefreshToken)];

    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AuditSaveChangesInterceptor(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CaptureAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        CaptureAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureAuditEntries(DbContext? context)
    {
        if (context is null) return;

        var (userId, userName) = GetCurrentUser();
        var ipAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var now = DateTime.UtcNow;

        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var entityType = entry.Entity.GetType();
            if (ExcludedTypes.Contains(entityType)) continue;
            if (entityType.Namespace != "AIHelpdesk.Domain.Entities") continue;

            // A soft-delete (IsDeleted flipped true) is semantically a delete, not an update.
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Deleted => AuditAction.Delete,
                _ when IsSoftDelete(entry) => AuditAction.Delete,
                _ => AuditAction.Update
            };

            // Unmodified updates (e.g. a re-saved entity with no actual property changes) aren't
            // worth a row.
            if (action == AuditAction.Update && !entry.Properties.Any(p => p.IsModified && !AreEqual(p.OriginalValue, p.CurrentValue)))
                continue;

            auditLogs.Add(new AuditLog
            {
                Timestamp = now,
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityName = entityType.Name,
                EntityId = GetEntityId(entry),
                Changes = BuildChangesJson(entry, action),
                IpAddress = ipAddress
            });
        }

        if (auditLogs.Count > 0)
            context.Set<AuditLog>().AddRange(auditLogs);
    }

    private static bool IsSoftDelete(EntityEntry entry)
    {
        var isDeletedProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "IsDeleted");
        return isDeletedProp is { IsModified: true } &&
               isDeletedProp.OriginalValue is false &&
               isDeletedProp.CurrentValue is true;
    }

    private static bool AreEqual(object? a, object? b) => Equals(a, b);

    private static string GetEntityId(EntityEntry entry)
    {
        var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        return idProp?.CurrentValue?.ToString() ?? "";
    }

    private static string BuildChangesJson(EntityEntry entry, AuditAction action)
    {
        Dictionary<string, object?> data;

        if (action == AuditAction.Update)
        {
            data = entry.Properties
                .Where(p => p.IsModified && !AreEqual(p.OriginalValue, p.CurrentValue))
                .ToDictionary(p => p.Metadata.Name, object? (p) => new { Old = p.OriginalValue, New = p.CurrentValue });
        }
        else
        {
            data = entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        }

        return JsonSerializer.Serialize(data);
    }

    private (Guid? userId, string? userName) GetCurrentUser()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return (null, null);

        // Candidate-portal tokens carry "sub" instead of the staff scheme's NameIdentifier claim
        // (see TokenService.GenerateCandidatePortalToken) -- check both so candidate-driven
        // mutations (document upload, slot booking) still attribute to a user.
        var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        var userId = Guid.TryParse(idClaim, out var id) ? id : (Guid?)null;
        var userName = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue(ClaimTypes.Name);
        return (userId, userName);
    }
}
