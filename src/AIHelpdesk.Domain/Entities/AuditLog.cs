using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

// Deliberately does not inherit BaseEntity: an audit row is an immutable fact about something
// else's mutation, not itself a mutable record with its own Created/UpdatedBy trail.
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;

    // JSON: { "PropertyName": { "Old": ..., "New": ... }, ... } for Update;
    // full property snapshot for Create/Delete.
    public string Changes { get; set; } = string.Empty;

    public string? IpAddress { get; set; }
}
