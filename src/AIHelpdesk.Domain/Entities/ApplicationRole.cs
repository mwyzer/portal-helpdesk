using Microsoft.AspNetCore.Identity;

namespace AIHelpdesk.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
