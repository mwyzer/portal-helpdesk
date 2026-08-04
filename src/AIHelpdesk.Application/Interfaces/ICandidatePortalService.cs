using AIHelpdesk.Contracts.Recruitment;

namespace AIHelpdesk.Application.Interfaces;

/// <summary>
/// Candidate-facing self-service portal — a deliberately separate surface from every other
/// staff-facing recruitment service. See TokenService's CandidatePortal JWT audience for how
/// this stays isolated from internal endpoints.
/// </summary>
public interface ICandidatePortalService
{
    Task<CandidatePortalAuthResponse> ActivateAccountAsync(CandidatePortalActivateRequest request);
    Task<CandidatePortalAuthResponse> LoginAsync(CandidatePortalLoginRequest request, string? ipAddress);
    Task<CandidatePortalAuthResponse> RefreshTokenAsync(CandidatePortalRefreshRequest request, string? ipAddress);
    Task LogoutAsync(string refreshToken);

    Task<CandidatePortalStatusResponse> GetMyStatusAsync(Guid candidateId);
    Task<IList<CandidateDocumentResponse>> GetMyDocumentsAsync(Guid candidateId);
    Task<CandidatePortalUploadDocumentResponse> UploadMyDocumentAsync(Guid candidateId, string fileName, string contentType, Stream fileStream);

    Task<IList<AvailableInterviewSlotResponse>> GetAvailableSlotsAsync(Guid candidateId);
    Task<CandidatePortalInterviewResponse> BookSlotAsync(Guid candidateId, Guid slotId);
    Task<IList<CandidatePortalInterviewResponse>> GetMyInterviewsAsync(Guid candidateId);
}
