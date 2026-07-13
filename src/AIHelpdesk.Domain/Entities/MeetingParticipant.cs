using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class MeetingParticipant : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Guid EmployeeId { get; set; }
    public ParticipantRole Role { get; set; } = ParticipantRole.Attendee;
    public bool IsRequired { get; set; }
    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Pending;

    public Meeting Meeting { get; set; } = null!;
    public ApplicationUser Employee { get; set; } = null!;
}
