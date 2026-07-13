namespace AIHelpdesk.Domain.Entities;

public class Department : Common.BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Position> Positions { get; set; } = new List<Position>();
}
