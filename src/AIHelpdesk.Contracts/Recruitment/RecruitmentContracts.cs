namespace AIHelpdesk.Contracts.Recruitment;

// ═══════════════ Job Vacancy ═══════════════

public record CreateJobVacancyRequest(
    string Title,
    string Description,
    string Requirements,
    Guid? DepartmentId,
    Guid? PositionId,
    int OpeningsCount);

public record UpdateJobVacancyRequest(
    string Title,
    string Description,
    string Requirements,
    Guid? DepartmentId,
    Guid? PositionId,
    int OpeningsCount);

public record JobVacancyResponse(
    Guid Id,
    string Title,
    string Description,
    string Requirements,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? PositionId,
    string? PositionName,
    int OpeningsCount,
    string Status,
    Guid PostedById,
    string PostedByName,
    int CandidateCount,
    DateTime? PublishedAt,
    DateTime? ClosedAt,
    DateTime CreatedAt);

// ═══════════════ Candidate ═══════════════

public record CreateCandidateRequest(
    Guid JobVacancyId,
    string FullName,
    string Email,
    string? Phone,
    string? Source);

public record UpdateCandidateRequest(
    string FullName,
    string Email,
    string? Phone,
    string? Source);

public record AdvanceCandidateStageRequest(string? Notes);

public record RejectCandidateRequest(string Reason);

public record CandidateResponse(
    Guid Id,
    Guid JobVacancyId,
    string JobVacancyTitle,
    string FullName,
    string Email,
    string? Phone,
    string? Source,
    string Stage,
    bool HasAISummary,
    DateTime CreatedAt);

public record CandidateStageHistoryResponse(
    Guid Id,
    string FromStage,
    string ToStage,
    Guid ChangedById,
    string ChangedByName,
    string? Notes,
    DateTime CreatedAt);

public record CandidateDocumentResponse(
    Guid Id,
    string FileName,
    long FileSize,
    string ContentType,
    Guid? UploadedById,
    string UploadedByName,
    DateTime CreatedAt);

public record InterviewSummaryResponse(
    Guid Id,
    DateTime ScheduledAt,
    string Type,
    string Status,
    string InterviewerName,
    int? Rating);

public record CandidateDetailResponse(
    Guid Id,
    Guid JobVacancyId,
    string JobVacancyTitle,
    string FullName,
    string Email,
    string? Phone,
    string? Source,
    string Stage,
    string? AISummaryJson,
    string? RejectionReason,
    List<CandidateDocumentResponse> Documents,
    List<CandidateStageHistoryResponse> StageHistory,
    List<InterviewSummaryResponse> Interviews,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ═══════════════ Interview ═══════════════

public record CreateInterviewRequest(
    Guid CandidateId,
    Guid InterviewerId,
    DateTime ScheduledAt,
    int DurationMinutes,
    string Type);

public record UpdateInterviewRequest(
    DateTime ScheduledAt,
    int DurationMinutes,
    string Type);

public record CompleteInterviewRequest(
    string Feedback,
    int Rating,
    string Recommendation);

public record InterviewResponse(
    Guid Id,
    Guid CandidateId,
    string CandidateName,
    Guid InterviewerId,
    string InterviewerName,
    DateTime ScheduledAt,
    int DurationMinutes,
    string Type,
    string Status,
    string? Feedback,
    int? Rating,
    string? Recommendation,
    DateTime? CompletedAt,
    DateTime CreatedAt);

public record InterviewQuestionResponse(
    Guid Id,
    string Question,
    string? Category,
    bool IsAIGenerated);

// ═══════════════ AI ═══════════════

public record CvSummarizeResponse(
    List<string> Skills,
    string? ExperienceSummary,
    string? EducationSummary,
    string RawSummary);

public record InterviewQuestionsResponse(List<InterviewQuestionResponse> Questions);

public record CandidateMatchResponse(
    Guid CandidateId,
    string CandidateName,
    double MatchScore,
    string Reason);

// ═══════════════ Interview Slots (staff-managed) ═══════════════

public record CreateInterviewSlotRequest(
    Guid InterviewerId,
    Guid JobVacancyId,
    DateTime ScheduledAt,
    int DurationMinutes,
    string Type);

public record InterviewSlotResponse(
    Guid Id,
    Guid InterviewerId,
    string InterviewerName,
    Guid JobVacancyId,
    string JobVacancyTitle,
    DateTime ScheduledAt,
    int DurationMinutes,
    string Type,
    string Status);

public record CandidatePortalInviteResponse(string SetupToken, DateTime ExpiresAt);

// ═══════════════ Candidate Portal (candidate-facing) ═══════════════

public record CandidatePortalLoginRequest(string Email, string Password);

public record CandidatePortalActivateRequest(string SetupToken, string NewPassword);

public record CandidatePortalRefreshRequest(string AccessToken, string RefreshToken);

public record CandidatePortalAuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    CandidatePortalProfile Profile);

public record CandidatePortalProfile(Guid CandidateId, string FullName, string Email);

public record CandidatePortalStatusResponse(
    string JobVacancyTitle,
    string Stage,
    string? RejectionReason,
    DateTime AppliedAt);

public record CandidatePortalUploadDocumentResponse(
    Guid Id,
    string FileName,
    long FileSize,
    DateTime CreatedAt);

public record AvailableInterviewSlotResponse(
    Guid SlotId,
    DateTime ScheduledAt,
    int DurationMinutes,
    string Type);

public record CandidatePortalInterviewResponse(
    Guid Id,
    DateTime ScheduledAt,
    int DurationMinutes,
    string Type,
    string Status);

// ═══════════════ Reports ═══════════════

public record RecruitmentStatsResponse(
    int TotalVacancies,
    int PublishedVacancies,
    int TotalCandidates,
    Dictionary<string, int> CandidatesPerStage,
    double AverageDaysInPipeline);
