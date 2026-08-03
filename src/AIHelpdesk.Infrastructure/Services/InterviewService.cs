using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Recruitment;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class InterviewService : IInterviewService
{
    private readonly ApplicationDbContext _context;

    public InterviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    private static InterviewResponse MapToResponse(Interview i) => new(
        i.Id, i.CandidateId, i.Candidate.FullName, i.InterviewerId, i.Interviewer.FullName,
        i.ScheduledAt, i.DurationMinutes, i.Type.ToString(), i.Status.ToString(),
        i.Feedback, i.Rating, i.Recommendation?.ToString(), i.CompletedAt, i.CreatedAt);

    private async Task EnsureNoConflictAsync(Guid interviewerId, DateTime scheduledAt, int durationMinutes, Guid? excludeInterviewId = null)
    {
        var newStart = scheduledAt;
        var newEnd = scheduledAt.AddMinutes(durationMinutes);

        var query = _context.Interviews.Where(i => i.InterviewerId == interviewerId && i.Status == InterviewStatus.Scheduled);
        if (excludeInterviewId.HasValue)
            query = query.Where(i => i.Id != excludeInterviewId.Value);

        var scheduled = await query.Select(i => new { i.ScheduledAt, i.DurationMinutes }).ToListAsync();
        var hasConflict = scheduled.Any(i =>
        {
            var existingStart = i.ScheduledAt;
            var existingEnd = i.ScheduledAt.AddMinutes(i.DurationMinutes);
            return newStart < existingEnd && existingStart < newEnd;
        });

        if (hasConflict)
            throw new InvalidOperationException("The interviewer already has a conflicting interview scheduled at this time");
    }

    public async Task<PagedResult<InterviewResponse>> GetAllAsync(int page, int pageSize, DateTime? fromDate, DateTime? toDate, Guid? candidateId)
    {
        var query = _context.Interviews
            .Include(i => i.Candidate)
            .Include(i => i.Interviewer)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(i => i.ScheduledAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(i => i.ScheduledAt <= toDate.Value);
        if (candidateId.HasValue)
            query = query.Where(i => i.CandidateId == candidateId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(i => i.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<InterviewResponse>(items.Select(MapToResponse).ToList(), totalCount, page, pageSize);
    }

    public async Task<InterviewResponse> GetByIdAsync(Guid id)
    {
        var interview = await _context.Interviews
            .Include(i => i.Candidate)
            .Include(i => i.Interviewer)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new KeyNotFoundException("Interview not found");

        return MapToResponse(interview);
    }

    public async Task<InterviewResponse> CreateAsync(CreateInterviewRequest request)
    {
        var candidate = await _context.Candidates.FindAsync(request.CandidateId)
            ?? throw new KeyNotFoundException("Candidate not found");

        if (!Enum.TryParse<InterviewType>(request.Type, true, out var type))
            throw new InvalidOperationException($"Invalid interview type: {request.Type}");

        await EnsureNoConflictAsync(request.InterviewerId, request.ScheduledAt, request.DurationMinutes);

        var interview = new Interview
        {
            CandidateId = request.CandidateId,
            InterviewerId = request.InterviewerId,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            Type = type,
            Status = InterviewStatus.Scheduled
        };

        _context.Interviews.Add(interview);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(interview.Id);
    }

    public async Task<InterviewResponse> UpdateAsync(Guid id, UpdateInterviewRequest request)
    {
        var interview = await _context.Interviews.FindAsync(id)
            ?? throw new KeyNotFoundException("Interview not found");

        if (interview.Status != InterviewStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled interviews can be rescheduled");

        if (!Enum.TryParse<InterviewType>(request.Type, true, out var type))
            throw new InvalidOperationException($"Invalid interview type: {request.Type}");

        await EnsureNoConflictAsync(interview.InterviewerId, request.ScheduledAt, request.DurationMinutes, id);

        interview.ScheduledAt = request.ScheduledAt;
        interview.DurationMinutes = request.DurationMinutes;
        interview.Type = type;
        interview.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<InterviewResponse> CompleteAsync(Guid id, CompleteInterviewRequest request)
    {
        var interview = await _context.Interviews.FindAsync(id)
            ?? throw new KeyNotFoundException("Interview not found");

        if (interview.Status != InterviewStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled interviews can be completed");

        if (!Enum.TryParse<InterviewRecommendation>(request.Recommendation, true, out var recommendation))
            throw new InvalidOperationException($"Invalid recommendation: {request.Recommendation}");

        interview.Status = InterviewStatus.Completed;
        interview.Feedback = request.Feedback;
        interview.Rating = request.Rating;
        interview.Recommendation = recommendation;
        interview.CompletedAt = DateTime.UtcNow;
        interview.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<InterviewResponse> CancelAsync(Guid id)
    {
        var interview = await _context.Interviews.FindAsync(id)
            ?? throw new KeyNotFoundException("Interview not found");

        if (interview.Status != InterviewStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled interviews can be cancelled");

        interview.Status = InterviewStatus.Cancelled;
        interview.CancelledAt = DateTime.UtcNow;
        interview.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<IList<InterviewResponse>> GetUpcomingAsync(Guid? interviewerId)
    {
        var now = DateTime.UtcNow;
        var query = _context.Interviews
            .Include(i => i.Candidate)
            .Include(i => i.Interviewer)
            .Where(i => i.Status == InterviewStatus.Scheduled && i.ScheduledAt >= now && i.ScheduledAt <= now.AddDays(7));

        if (interviewerId.HasValue)
            query = query.Where(i => i.InterviewerId == interviewerId.Value);

        var items = await query.OrderBy(i => i.ScheduledAt).ToListAsync();
        return items.Select(MapToResponse).ToList();
    }
}
