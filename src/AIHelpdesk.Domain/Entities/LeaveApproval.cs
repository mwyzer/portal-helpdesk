using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class LeaveApproval : BaseEntity
{
    public Guid LeaveRequestId { get; set; }
    public Guid ApproverId { get; set; }
    public string ApproverRole { get; set; } = string.Empty;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string? Note { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public LeaveRequest LeaveRequest { get; set; } = null!;
    public ApplicationUser Approver { get; set; } = null!;
}
