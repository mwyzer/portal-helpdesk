using System.IdentityModel.Tokens.Jwt;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Recruitment;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AIHelpdesk.Infrastructure.Services;

public class CandidatePortalService : ICandidatePortalService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IInterviewService _interviewService;
    private readonly IPasswordHasher<CandidateAccount> _passwordHasher = new PasswordHasher<CandidateAccount>();
    private readonly string _uploadPath;

    public CandidatePortalService(
        ApplicationDbContext context, ITokenService tokenService, IInterviewService interviewService, IConfiguration configuration)
    {
        _context = context;
        _tokenService = tokenService;
        _interviewService = interviewService;
        _uploadPath = configuration["Recruitment:UploadPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads", "candidates");
        Directory.CreateDirectory(_uploadPath);
    }

    private async Task<CandidatePortalAuthResponse> GenerateAuthResponseAsync(Candidate candidate, string? ipAddress)
    {
        var accessToken = _tokenService.GenerateCandidatePortalToken(candidate);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _context.CandidatePortalRefreshTokens.Add(new CandidatePortalRefreshToken
        {
            CandidateId = candidate.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress
        });
        await _context.SaveChangesAsync();

        return new CandidatePortalAuthResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(15),
            new CandidatePortalProfile(candidate.Id, candidate.FullName, candidate.Email));
    }

    public async Task<CandidatePortalAuthResponse> ActivateAccountAsync(CandidatePortalActivateRequest request)
    {
        var account = await _context.CandidateAccounts
            .Include(a => a.Candidate)
            .FirstOrDefaultAsync(a => a.SetupToken == request.SetupToken)
            ?? throw new InvalidOperationException("Invalid or expired activation link");

        if (account.SetupTokenExpiresAt == null || account.SetupTokenExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired activation link");

        account.PasswordHash = _passwordHasher.HashPassword(account, request.NewPassword);
        account.IsActive = true;
        account.ActivatedAt = DateTime.UtcNow;
        account.LastLoginAt = DateTime.UtcNow;
        account.SetupToken = null;
        account.SetupTokenExpiresAt = null;
        await _context.SaveChangesAsync();

        return await GenerateAuthResponseAsync(account.Candidate, ipAddress: null);
    }

    public async Task<CandidatePortalAuthResponse> LoginAsync(CandidatePortalLoginRequest request, string? ipAddress)
    {
        var account = await _context.CandidateAccounts
            .Include(a => a.Candidate)
            .FirstOrDefaultAsync(a => a.Candidate.Email == request.Email);

        if (account == null || !account.IsActive || account.PasswordHash == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        var verifyResult = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid email or password");

        account.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GenerateAuthResponseAsync(account.Candidate, ipAddress);
    }

    public async Task<CandidatePortalAuthResponse> RefreshTokenAsync(CandidatePortalRefreshRequest request, string? ipAddress)
    {
        var principal = _tokenService.ValidateCandidatePortalToken(request.AccessToken)
            ?? throw new UnauthorizedAccessException("Invalid access token");

        var candidateId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? throw new UnauthorizedAccessException("Invalid token claims");

        var refreshToken = await _context.CandidatePortalRefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.CandidateId.ToString() == candidateId);

        if (refreshToken == null || !refreshToken.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;

        var candidate = await _context.Candidates.FindAsync(Guid.Parse(candidateId))
            ?? throw new UnauthorizedAccessException("Candidate not found");

        return await GenerateAuthResponseAsync(candidate, ipAddress);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var token = await _context.CandidatePortalRefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (token != null)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<CandidatePortalStatusResponse> GetMyStatusAsync(Guid candidateId)
    {
        var candidate = await _context.Candidates.Include(c => c.JobVacancy)
            .FirstOrDefaultAsync(c => c.Id == candidateId)
            ?? throw new KeyNotFoundException("Candidate not found");

        return new CandidatePortalStatusResponse(
            candidate.JobVacancy.Title, candidate.Stage.ToString(), candidate.RejectionReason, candidate.CreatedAt);
    }

    public async Task<IList<CandidateDocumentResponse>> GetMyDocumentsAsync(Guid candidateId)
    {
        var documents = await _context.CandidateDocuments
            .Include(d => d.UploadedBy)
            .Where(d => d.CandidateId == candidateId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return documents.Select(d => new CandidateDocumentResponse(
            d.Id, d.FileName, d.FileSize, d.ContentType, d.UploadedById,
            d.UploadedBy?.FullName ?? "You", d.CreatedAt)).ToList();
    }

    public async Task<CandidatePortalUploadDocumentResponse> UploadMyDocumentAsync(
        Guid candidateId, string fileName, string contentType, Stream fileStream)
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
            UploadedById = null // candidate self-upload — see CandidateDocument.UploadedById
        };

        _context.CandidateDocuments.Add(document);
        await _context.SaveChangesAsync();

        return new CandidatePortalUploadDocumentResponse(document.Id, document.FileName, document.FileSize, document.CreatedAt);
    }

    public async Task<IList<AvailableInterviewSlotResponse>> GetAvailableSlotsAsync(Guid candidateId)
    {
        var candidate = await _context.Candidates.FindAsync(candidateId)
            ?? throw new KeyNotFoundException("Candidate not found");

        var slots = await _context.InterviewSlots
            .Where(s => s.JobVacancyId == candidate.JobVacancyId
                && s.Status == InterviewSlotStatus.Open
                && s.ScheduledAt > DateTime.UtcNow)
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync();

        return slots.Select(s => new AvailableInterviewSlotResponse(s.Id, s.ScheduledAt, s.DurationMinutes, s.Type.ToString())).ToList();
    }

    public async Task<CandidatePortalInterviewResponse> BookSlotAsync(Guid candidateId, Guid slotId)
    {
        // Delegates to InterviewService's booking transaction (conflict re-check, atomic
        // conditional claim) -- this service only owns candidate-facing authorization/scoping,
        // not interview-scheduling invariants.
        var interview = await _interviewService.BookSlotAsync(slotId, candidateId);

        return new CandidatePortalInterviewResponse(
            interview.Id, interview.ScheduledAt, interview.DurationMinutes, interview.Type, interview.Status);
    }

    public async Task<IList<CandidatePortalInterviewResponse>> GetMyInterviewsAsync(Guid candidateId)
    {
        var interviews = await _context.Interviews
            .Where(i => i.CandidateId == candidateId)
            .OrderByDescending(i => i.ScheduledAt)
            .ToListAsync();

        return interviews.Select(i => new CandidatePortalInterviewResponse(
            i.Id, i.ScheduledAt, i.DurationMinutes, i.Type.ToString(), i.Status.ToString())).ToList();
    }
}
