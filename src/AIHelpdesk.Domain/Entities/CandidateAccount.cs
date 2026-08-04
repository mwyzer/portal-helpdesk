using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

/// <summary>
/// A candidate's self-service portal login. Deliberately not an <see cref="ApplicationUser"/> —
/// candidates are external and must never satisfy internal `[Authorize]` checks. See the
/// candidate-portal JWT audience isolation in Program.cs / TokenService.
/// </summary>
public class CandidateAccount : BaseEntity
{
    public Guid CandidateId { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; }
    public DateTime? InvitedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? SetupToken { get; set; }
    public DateTime? SetupTokenExpiresAt { get; set; }

    public Candidate Candidate { get; set; } = null!;
}
