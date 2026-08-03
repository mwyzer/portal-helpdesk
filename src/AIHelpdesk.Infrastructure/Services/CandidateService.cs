using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Excel;
using AIHelpdesk.Contracts.Recruitment;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AIHelpdesk.Infrastructure.Services;

public class CandidateService : ICandidateService
{
    // Forward pipeline order — AdvanceStageAsync only ever moves to the next entry.
    // Rejected is intentionally excluded: it's a terminal state reachable from any active
    // stage via RejectAsync, not part of the forward sequence.
    private static readonly CandidateStage[] ForwardOrder =
    [
        CandidateStage.Applied, CandidateStage.Screening, CandidateStage.Test,
        CandidateStage.Interview, CandidateStage.Offering, CandidateStage.Hired
    ];

    private static readonly HashSet<string> AllowedCvExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx" };
    private const long MaxCvSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly ApplicationDbContext _context;
    private readonly IExcelService _excel;
    private readonly string _uploadPath;

    public CandidateService(ApplicationDbContext context, IConfiguration configuration, IExcelService excel)
    {
        _context = context;
        _excel = excel;
        _uploadPath = configuration["Recruitment:UploadPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads", "candidates");
        Directory.CreateDirectory(_uploadPath);
    }

    private static CandidateResponse MapToResponse(Candidate c) => new(
        c.Id, c.JobVacancyId, c.JobVacancy.Title, c.FullName, c.Email, c.Phone, c.Source,
        c.Stage.ToString(), !string.IsNullOrEmpty(c.AISummaryJson), c.CreatedAt);

    public async Task<PagedResult<CandidateResponse>> GetAllAsync(int page, int pageSize, string? stage, Guid? jobVacancyId)
    {
        var query = _context.Candidates.Include(c => c.JobVacancy).AsQueryable();

        if (!string.IsNullOrWhiteSpace(stage) && Enum.TryParse<CandidateStage>(stage, true, out var s))
            query = query.Where(c => c.Stage == s);
        if (jobVacancyId.HasValue)
            query = query.Where(c => c.JobVacancyId == jobVacancyId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<CandidateResponse>(items.Select(MapToResponse).ToList(), totalCount, page, pageSize);
    }

    public async Task<CandidateDetailResponse> GetByIdAsync(Guid id)
    {
        var c = await _context.Candidates
            .Include(x => x.JobVacancy)
            .Include(x => x.Documents).ThenInclude(d => d.UploadedBy)
            .Include(x => x.StageHistory).ThenInclude(h => h.ChangedBy)
            .Include(x => x.Interviews).ThenInclude(i => i.Interviewer)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Candidate not found");

        return new CandidateDetailResponse(
            c.Id, c.JobVacancyId, c.JobVacancy.Title, c.FullName, c.Email, c.Phone, c.Source,
            c.Stage.ToString(), c.AISummaryJson, c.RejectionReason,
            c.Documents.Select(d => new CandidateDocumentResponse(
                d.Id, d.FileName, d.FileSize, d.ContentType, d.UploadedById, d.UploadedBy.FullName, d.CreatedAt)).ToList(),
            c.StageHistory.OrderByDescending(h => h.CreatedAt).Select(h => new CandidateStageHistoryResponse(
                h.Id, h.FromStage.ToString(), h.ToStage.ToString(), h.ChangedById, h.ChangedBy.FullName, h.Notes, h.CreatedAt)).ToList(),
            c.Interviews.OrderByDescending(i => i.ScheduledAt).Select(i => new InterviewSummaryResponse(
                i.Id, i.ScheduledAt, i.Type.ToString(), i.Status.ToString(), i.Interviewer.FullName, i.Rating)).ToList(),
            c.CreatedAt, c.UpdatedAt);
    }

    public async Task<CandidateResponse> CreateAsync(CreateCandidateRequest request)
    {
        var vacancy = await _context.JobVacancies.FindAsync(request.JobVacancyId)
            ?? throw new KeyNotFoundException("Job vacancy not found");

        var candidate = new Candidate
        {
            JobVacancyId = request.JobVacancyId,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Source = request.Source,
            Stage = CandidateStage.Applied
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        candidate.JobVacancy = vacancy;
        return MapToResponse(candidate);
    }

    public async Task<CandidateResponse> UpdateAsync(Guid id, UpdateCandidateRequest request)
    {
        var candidate = await _context.Candidates.Include(c => c.JobVacancy).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Candidate not found");

        candidate.FullName = request.FullName;
        candidate.Email = request.Email;
        candidate.Phone = request.Phone;
        candidate.Source = request.Source;
        candidate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToResponse(candidate);
    }

    public async Task<CandidateDocumentResponse> UploadCvAsync(Guid candidateId, Guid userId, string fileName, string contentType, Stream fileStream)
    {
        var candidate = await _context.Candidates.FindAsync(candidateId)
            ?? throw new KeyNotFoundException("Candidate not found");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedCvExtensions.Contains(extension))
            throw new InvalidOperationException("Only PDF and DOCX files are allowed for CVs");

        if (fileStream.CanSeek && fileStream.Length > MaxCvSizeBytes)
            throw new InvalidOperationException("CV file exceeds the maximum allowed size of 5 MB");

        var safeFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadPath, safeFileName);
        await using (var output = File.Create(filePath))
        {
            await fileStream.CopyToAsync(output);
        }

        var document = new CandidateDocument
        {
            CandidateId = candidateId,
            FileName = fileName,
            FilePath = filePath,
            FileSize = new FileInfo(filePath).Length,
            ContentType = contentType,
            UploadedById = userId
        };

        _context.CandidateDocuments.Add(document);
        await _context.SaveChangesAsync();

        var uploader = await _context.Users.FindAsync(userId);
        return new CandidateDocumentResponse(
            document.Id, document.FileName, document.FileSize, document.ContentType,
            document.UploadedById, uploader?.FullName ?? "Unknown", document.CreatedAt);
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadCvAsync(Guid candidateId, Guid documentId)
    {
        var document = await _context.CandidateDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CandidateId == candidateId)
            ?? throw new KeyNotFoundException("Document not found");

        if (!File.Exists(document.FilePath))
            throw new KeyNotFoundException("Document file is missing from storage");

        Stream stream = File.OpenRead(document.FilePath);
        return (stream, document.ContentType, document.FileName);
    }

    public async Task<CandidateResponse> AdvanceStageAsync(Guid id, Guid userId, AdvanceCandidateStageRequest request)
    {
        var candidate = await _context.Candidates.Include(c => c.JobVacancy).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Candidate not found");

        var currentIndex = Array.IndexOf(ForwardOrder, candidate.Stage);
        if (currentIndex < 0 || currentIndex >= ForwardOrder.Length - 1)
            throw new InvalidOperationException($"Cannot advance a candidate from the '{candidate.Stage}' stage");

        var fromStage = candidate.Stage;
        var toStage = ForwardOrder[currentIndex + 1];
        candidate.Stage = toStage;
        candidate.UpdatedAt = DateTime.UtcNow;

        _context.CandidateStageHistories.Add(new CandidateStageHistory
        {
            CandidateId = id,
            FromStage = fromStage,
            ToStage = toStage,
            ChangedById = userId,
            Notes = request.Notes
        });

        await _context.SaveChangesAsync();
        return MapToResponse(candidate);
    }

    public async Task<CandidateResponse> RejectAsync(Guid id, Guid userId, RejectCandidateRequest request)
    {
        var candidate = await _context.Candidates.Include(c => c.JobVacancy).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Candidate not found");

        if (candidate.Stage is CandidateStage.Hired or CandidateStage.Rejected)
            throw new InvalidOperationException($"Cannot reject a candidate who is already '{candidate.Stage}'");

        var fromStage = candidate.Stage;
        candidate.Stage = CandidateStage.Rejected;
        candidate.RejectionReason = request.Reason;
        candidate.UpdatedAt = DateTime.UtcNow;

        _context.CandidateStageHistories.Add(new CandidateStageHistory
        {
            CandidateId = id,
            FromStage = fromStage,
            ToStage = CandidateStage.Rejected,
            ChangedById = userId,
            Notes = request.Reason
        });

        await _context.SaveChangesAsync();
        return MapToResponse(candidate);
    }

    public async Task<RecruitmentStatsResponse> GetStatsAsync()
    {
        var totalVacancies = await _context.JobVacancies.CountAsync();
        var publishedVacancies = await _context.JobVacancies.CountAsync(v => v.Status == VacancyStatus.Published);
        var totalCandidates = await _context.Candidates.CountAsync();

        var candidatesPerStage = await _context.Candidates
            .GroupBy(c => c.Stage)
            .Select(g => new { Stage = g.Key, Count = g.Count() })
            .ToListAsync();
        var stageDict = Enum.GetValues<CandidateStage>()
            .ToDictionary(s => s.ToString(), s => candidatesPerStage.FirstOrDefault(g => g.Stage == s)?.Count ?? 0);

        var completed = await _context.Candidates
            .Where(c => c.Stage == CandidateStage.Hired || c.Stage == CandidateStage.Rejected)
            .Select(c => new { c.CreatedAt, c.UpdatedAt })
            .ToListAsync();
        var averageDays = completed.Count > 0
            ? completed.Average(c => (c.UpdatedAt - c.CreatedAt).TotalDays)
            : 0;

        return new RecruitmentStatsResponse(totalVacancies, publishedVacancies, totalCandidates, stageDict, Math.Round(averageDays, 1));
    }

    public async Task<byte[]> ExportToExcelAsync(Guid? jobVacancyId, string? stage)
    {
        var query = _context.Candidates.Include(c => c.JobVacancy).AsQueryable();

        if (jobVacancyId.HasValue)
            query = query.Where(c => c.JobVacancyId == jobVacancyId.Value);
        if (!string.IsNullOrWhiteSpace(stage) && Enum.TryParse<CandidateStage>(stage, true, out var s))
            query = query.Where(c => c.Stage == s);

        var candidates = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        var config = new ExcelExportConfig("Candidates", [
            new ExcelColumnDefinition("Full Name", "FullName", 25),
            new ExcelColumnDefinition("Email", "Email", 30),
            new ExcelColumnDefinition("Phone", "Phone", 18),
            new ExcelColumnDefinition("Vacancy", "Vacancy", 25),
            new ExcelColumnDefinition("Stage", "Stage", 14),
            new ExcelColumnDefinition("Source", "Source", 16),
            new ExcelColumnDefinition("Applied At", "AppliedAt", 18, "yyyy-MM-dd"),
        ]);

        return await _excel.ExportToExcelAsync(
            candidates,
            config,
            (c, col) => col.PropertyName switch
            {
                "FullName" => c.FullName,
                "Email" => c.Email,
                "Phone" => c.Phone,
                "Vacancy" => c.JobVacancy.Title,
                "Stage" => c.Stage.ToString(),
                "Source" => c.Source,
                "AppliedAt" => c.CreatedAt,
                _ => null,
            });
    }
}
