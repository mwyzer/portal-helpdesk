using System.Text.Json;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Recruitment;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIHelpdesk.Infrastructure.Services;

/// <summary>
/// AI-assisted recruitment features: CV summarization, interview question generation, and
/// candidate-job matching. All AI calls degrade gracefully (empty/default result) on failure
/// rather than surfacing a 500 to the caller — recruitment workflows should stay usable even
/// when the AI provider is unavailable.
/// </summary>
public class RecruitmentAIService : IRecruitmentAIService
{
    private const string CvSummaryPrompt =
        "You are an HR assistant. Extract structured information from the candidate CV text below. " +
        "Respond with ONLY a JSON object, no markdown, in this exact shape: " +
        "{\"skills\":[\"skill1\",\"skill2\"],\"experienceSummary\":\"<one paragraph>\",\"educationSummary\":\"<one paragraph>\"}";

    private const string InterviewQuestionsPrompt =
        "You are an interview panel assistant. Given the job requirements and (if available) the candidate's " +
        "CV summary below, generate 5 to 8 relevant interview questions covering technical and behavioral aspects. " +
        "Respond with ONLY a JSON array, no markdown, in this exact shape: " +
        "[{\"question\":\"<question text>\",\"category\":\"Technical|Behavioral|Situational\"}]";

    private const string MatchPrompt =
        "You are a recruitment assistant. Compare the candidate's CV summary against the job requirements below. " +
        "Respond with ONLY a JSON object, no markdown, in this exact shape: " +
        "{\"score\":<number between 0 and 1>,\"reason\":\"<one sentence why>\"}";

    private readonly ApplicationDbContext _context;
    private readonly IAIService _ai;
    private readonly ILogger<RecruitmentAIService> _logger;

    public RecruitmentAIService(ApplicationDbContext context, IAIService ai, ILogger<RecruitmentAIService> logger)
    {
        _context = context;
        _ai = ai;
        _logger = logger;
    }

    public async Task<CvSummarizeResponse> SummarizeCvAsync(Guid candidateId)
    {
        var candidate = await _context.Candidates
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == candidateId)
            ?? throw new KeyNotFoundException("Candidate not found");

        var cv = candidate.Documents.OrderByDescending(d => d.CreatedAt).FirstOrDefault()
            ?? throw new InvalidOperationException("Candidate has no CV uploaded");

        var cvText = await ExtractTextAsync(cv.FilePath, Path.GetExtension(cv.FileName));

        // Feeding an empty document to the model invites it to fabricate a plausible-looking
        // but entirely made-up summary instead of reporting that there was nothing to read
        // (observed with scanned/image-only PDFs, which have no text layer to extract).
        if (string.IsNullOrWhiteSpace(cvText))
        {
            _logger.LogWarning("No extractable text found in CV for candidate {CandidateId} ({FilePath})", candidateId, cv.FilePath);
            return new CvSummarizeResponse([], null, null, "Could not extract readable text from this CV file. It may be a scanned image without a text layer.");
        }

        try
        {
            var raw = await _ai.GenerateChatResponseAsync(CvSummaryPrompt, cvText, []);
            var parsed = ParseJson<CvSummaryDto>(raw);

            if (parsed != null)
            {
                candidate.AISummaryJson = raw;
                candidate.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new CvSummarizeResponse(
                    parsed.Skills ?? [], parsed.ExperienceSummary, parsed.EducationSummary, raw);
            }

            _logger.LogWarning("CV summarization returned unparseable content for candidate {CandidateId}", candidateId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CV summarization failed for candidate {CandidateId}", candidateId);
        }

        return new CvSummarizeResponse([], null, null, "AI summarization unavailable.");
    }

    public async Task<InterviewQuestionsResponse> GenerateInterviewQuestionsAsync(Guid interviewId)
    {
        var interview = await _context.Interviews
            .Include(i => i.Candidate).ThenInclude(c => c.JobVacancy)
            .FirstOrDefaultAsync(i => i.Id == interviewId)
            ?? throw new KeyNotFoundException("Interview not found");

        var userMessage = $"Job requirements:\n{interview.Candidate.JobVacancy.Requirements}\n\n" +
            $"Candidate CV summary:\n{interview.Candidate.AISummaryJson ?? "(not summarized yet)"}";

        var questions = new List<InterviewQuestion>();

        try
        {
            var raw = await _ai.GenerateChatResponseAsync(InterviewQuestionsPrompt, userMessage, []);
            var parsed = ParseJsonArray<InterviewQuestionDto>(raw);

            if (parsed != null && parsed.Count > 0)
            {
                foreach (var q in parsed)
                {
                    var question = new InterviewQuestion
                    {
                        InterviewId = interviewId,
                        Question = q.Question,
                        Category = q.Category,
                        IsAIGenerated = true
                    };
                    _context.InterviewQuestions.Add(question);
                    questions.Add(question);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Interview question generation returned unparseable content for interview {InterviewId}", interviewId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Interview question generation failed for interview {InterviewId}", interviewId);
        }

        return new InterviewQuestionsResponse(
            questions.Select(q => new InterviewQuestionResponse(q.Id, q.Question, q.Category, q.IsAIGenerated)).ToList());
    }

    public async Task<IList<CandidateMatchResponse>> MatchCandidatesAsync(Guid jobVacancyId)
    {
        var vacancy = await _context.JobVacancies.FindAsync(jobVacancyId)
            ?? throw new KeyNotFoundException("Job vacancy not found");

        var candidates = await _context.Candidates
            .Where(c => c.JobVacancyId == jobVacancyId)
            .ToListAsync();

        var results = new List<CandidateMatchResponse>();

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate.AISummaryJson))
            {
                results.Add(new CandidateMatchResponse(candidate.Id, candidate.FullName, 0, "No CV summary available yet."));
                continue;
            }

            try
            {
                var userMessage = $"Job requirements:\n{vacancy.Requirements}\n\nCandidate CV summary:\n{candidate.AISummaryJson}";
                var raw = await _ai.GenerateChatResponseAsync(MatchPrompt, userMessage, []);
                var parsed = ParseJson<MatchDto>(raw);

                results.Add(parsed != null
                    ? new CandidateMatchResponse(candidate.Id, candidate.FullName, parsed.Score, parsed.Reason)
                    : new CandidateMatchResponse(candidate.Id, candidate.FullName, 0, "AI match unavailable."));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Candidate match failed for candidate {CandidateId}", candidate.Id);
                results.Add(new CandidateMatchResponse(candidate.Id, candidate.FullName, 0, "AI match unavailable."));
            }
        }

        return results.OrderByDescending(r => r.MatchScore).ToList();
    }

    private async Task<string> ExtractTextAsync(string filePath, string extension)
    {
        if (!File.Exists(filePath)) return string.Empty;

        return extension.ToLowerInvariant() switch
        {
            ".docx" => ExtractDocxText(filePath),
            _ => await ExtractPdfText(filePath)
        };
    }

    private Task<string> ExtractPdfText(string filePath)
    {
        // Real PDF parsing via PdfPig — the overwhelming majority of real-world PDFs (anything
        // exported from Word, Google Docs, Canva, etc.) use compressed (FlateDecode) content
        // streams, which a raw-bytes/regex scan for "(...) Tj" can never see since the text
        // operators only exist after decompression. That previously meant real CVs silently
        // extracted to an empty string, and the LLM would fabricate a plausible-sounding summary
        // from nothing rather than reflect the candidate's actual CV content.
        try
        {
            using var document = UglyToad.PdfPig.PdfDocument.Open(filePath);
            var text = new System.Text.StringBuilder();
            foreach (var page in document.GetPages())
                text.AppendLine(page.Text);
            return Task.FromResult(text.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse PDF {FilePath}", filePath);
            return Task.FromResult(string.Empty);
        }
    }

    private static string ExtractDocxText(string filePath)
    {
        try
        {
            using var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false);
            return wordDoc.MainDocumentPart?.Document.Body?.InnerText ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static T? ParseJson<T>(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start) return default;

        try
        {
            return JsonSerializer.Deserialize<T>(raw[start..(end + 1)], new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static List<T>? ParseJsonArray<T>(string raw)
    {
        var start = raw.IndexOf('[');
        var end = raw.LastIndexOf(']');
        if (start < 0 || end <= start) return null;

        try
        {
            return JsonSerializer.Deserialize<List<T>>(raw[start..(end + 1)], new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private record CvSummaryDto(List<string>? Skills, string? ExperienceSummary, string? EducationSummary);
    private record InterviewQuestionDto(string Question, string? Category);
    private record MatchDto(double Score, string Reason);
}
