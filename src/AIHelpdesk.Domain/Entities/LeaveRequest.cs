using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class LeaveRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Draft;
    public string? AttachmentUrl { get; set; }
    public string? RejectionReason { get; set; }

    public Employee Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
    public ICollection<LeaveApproval> Approvals { get; set; } = new List<LeaveApproval>();
}
