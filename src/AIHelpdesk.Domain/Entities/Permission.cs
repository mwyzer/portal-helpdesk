namespace AIHelpdesk.Domain.Entities;

public class Permission : Common.BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();
}
