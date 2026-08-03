using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class AgentAssignment : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxTickets { get; set; } = 10;
    public int CurrentLoad { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Department Department { get; set; } = null!;
}
