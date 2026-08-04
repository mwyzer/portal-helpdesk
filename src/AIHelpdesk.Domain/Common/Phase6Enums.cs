namespace AIHelpdesk.Domain.Common;

public enum VacancyStatus
{
    Draft,
    Published,
    Closed,
    Filled
}

/// <summary>
/// Ordered pipeline stages. Order matters — CandidateService uses the declaration order to
/// enforce "advance one stage at a time" (no skipping). Rejected is a terminal state reachable
/// from any active stage, not part of the forward sequence.
/// </summary>
public enum CandidateStage
{
    Applied,
    Screening,
    Test,
    Interview,
    Offering,
    Hired,
    Rejected
}

public enum InterviewType
{
    Phone,
    Video,
    OnSite
}

public enum InterviewStatus
{
    Scheduled,
    Completed,
    Cancelled
}

public enum InterviewRecommendation
{
    StrongYes,
    Yes,
    No,
    StrongNo
}

public enum InterviewSlotStatus
{
    Open,
    Booked,
    Cancelled
}
