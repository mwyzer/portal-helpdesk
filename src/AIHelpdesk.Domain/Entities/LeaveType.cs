using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class LeaveType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DaysPerYear { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int MinServiceMonths { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool SkipManagerApproval { get; set; }
}
