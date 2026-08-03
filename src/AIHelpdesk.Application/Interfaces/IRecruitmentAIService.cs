using AIHelpdesk.Contracts.Recruitment;

namespace AIHelpdesk.Application.Interfaces;

public interface IRecruitmentAIService
{
    Task<CvSummarizeResponse> SummarizeCvAsync(Guid candidateId);
    Task<InterviewQuestionsResponse> GenerateInterviewQuestionsAsync(Guid interviewId);
    Task<IList<CandidateMatchResponse>> MatchCandidatesAsync(Guid jobVacancyId);
}
