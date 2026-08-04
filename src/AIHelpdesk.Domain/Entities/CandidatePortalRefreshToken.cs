using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

/// <summary>
/// Refresh token for the candidate self-service portal. A separate table from
/// <see cref="RefreshToken"/> because that one's UserId is FK-constrained to
/// <see cref="ApplicationUser"/> (AspNetUsers) -- a Candidate.Id would violate that constraint.
/// Same shape/rotation semantics as RefreshToken, just scoped to Candidate instead.
/// </summary>
public class CandidatePortalRefreshToken : BaseEntity
{
    public Guid CandidateId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? CreatedByIp { get; set; }

    public Candidate Candidate { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
