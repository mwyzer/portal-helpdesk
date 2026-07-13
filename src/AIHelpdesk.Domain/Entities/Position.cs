namespace AIHelpdesk.Domain.Entities;

public class Position : Common.BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public Department Department { get; set; } = null!;
}
