using System.Security.Cryptography;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Excel;
using AIHelpdesk.Contracts.Recruitment;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

    private static readonly TimeSpan SetupTokenLifetime = TimeSpan.FromDays(7);

    private readonly ApplicationDbContext _context;
    private readonly IExcelService _excel;
    private readonly ILogger<CandidateService> _logger;
    private readonly string _uploadPath;

    public CandidateService(ApplicationDbContext context, IConfiguration configuration, IExcelService excel, ILogger<CandidateService> logger)
    {
        _context = context;
        _excel = excel;
        _logger = logger;
        _uploadPath = configuration["Recruitment:UploadPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads", "candidates");
        Directory.CreateDirectory(_uploadPath);
    }

    private static string GenerateSetupToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
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
                d.Id, d.FileName, d.FileSize, d.ContentType, d.UploadedById, d.UploadedBy?.FullName ?? "Candidate (self-uploaded)", d.CreatedAt)).ToList(),
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

        var account = new CandidateAccount
        {
            CandidateId = candidate.Id,
            IsActive = false,
            InvitedAt = DateTime.UtcNow,
            SetupToken = GenerateSetupToken(),
            SetupTokenExpiresAt = DateTime.UtcNow.Add(SetupTokenLifetime)
        };
        _context.CandidateAccounts.Add(account);
        await _context.SaveChangesAsync();

        // No SMTP provider is configured yet (same limitation as AuthService.ForgotPasswordAsync)
        // -- log the setup token so the portal invite flow is testable end-to-end, and staff can
        // also fetch/regenerate it via RegenerateInviteAsync to copy into a manually-sent message.
        _logger.LogInformation(
            "Candidate portal invite created for {Email}. Setup token: {Token}", candidate.Email, account.SetupToken);

        candidate.JobVacancy = vacancy;
        return MapToResponse(candidate);
    }

    public async Task<CandidatePortalInviteResponse> RegenerateInviteAsync(Guid candidateId)
    {
        var candidate = await _context.Candidates.FindAsync(candidateId)
            ?? throw new KeyNotFoundException("Candidate not found");

        var account = await _context.CandidateAccounts.FirstOrDefaultAsync(a => a.CandidateId == candidateId);
        if (account == null)
        {
            account = new CandidateAccount { CandidateId = candidateId, IsActive = false };
            _context.CandidateAccounts.Add(account);
        }

        account.SetupToken = GenerateSetupToken();
        account.SetupTokenExpiresAt = DateTime.UtcNow.Add(SetupTokenLifetime);
        account.InvitedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Candidate portal invite regenerated for {Email}. Setup token: {Token}", candidate.Email, account.SetupToken);

        return new CandidatePortalInviteResponse(account.SetupToken, account.SetupTokenExpiresAt.Value);
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

        RecruitmentFileValidation.EnsureValid(fileName, fileStream);

        var extension = Path.GetExtension(fileName);
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

    public async Task DeleteCvAsync(Guid candidateId, Guid documentId)
    {
        var document = await _context.CandidateDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.CandidateId == candidateId)
            ?? throw new KeyNotFoundException("Document not found");

        _context.CandidateDocuments.Remove(document);
        await _context.SaveChangesAsync();

        if (File.Exists(document.FilePath))
        {
            try { File.Delete(document.FilePath); }
            catch (IOException ex) { _logger.LogWarning(ex, "Failed to delete CV file {FilePath}", document.FilePath); }
        }
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
