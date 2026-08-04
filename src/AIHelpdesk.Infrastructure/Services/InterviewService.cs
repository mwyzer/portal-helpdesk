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

    private static InterviewSlotResponse MapSlotToResponse(InterviewSlot s) => new(
        s.Id, s.InterviewerId, s.Interviewer.FullName, s.JobVacancyId, s.JobVacancy.Title,
        s.ScheduledAt, s.DurationMinutes, s.Type.ToString(), s.Status.ToString());

    public async Task<InterviewSlotResponse> CreateSlotAsync(CreateInterviewSlotRequest request)
    {
        _ = await _context.JobVacancies.FindAsync(request.JobVacancyId)
            ?? throw new KeyNotFoundException("Job vacancy not found");
        _ = await _context.Users.FindAsync(request.InterviewerId)
            ?? throw new KeyNotFoundException("Interviewer not found");

        if (!Enum.TryParse<InterviewType>(request.Type, true, out var type))
            throw new InvalidOperationException($"Invalid interview type: {request.Type}");

        await EnsureNoConflictAsync(request.InterviewerId, request.ScheduledAt, request.DurationMinutes);

        var slot = new InterviewSlot
        {
            InterviewerId = request.InterviewerId,
            JobVacancyId = request.JobVacancyId,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            Type = type,
            Status = InterviewSlotStatus.Open
        };

        _context.InterviewSlots.Add(slot);
        await _context.SaveChangesAsync();

        slot.Interviewer = await _context.Users.FindAsync(request.InterviewerId) ?? slot.Interviewer;
        slot.JobVacancy = await _context.JobVacancies.FindAsync(request.JobVacancyId) ?? slot.JobVacancy;
        return MapSlotToResponse(slot);
    }

    public async Task<IList<InterviewSlotResponse>> GetSlotsAsync(Guid? jobVacancyId, Guid? interviewerId, string? status)
    {
        var query = _context.InterviewSlots
            .Include(s => s.Interviewer)
            .Include(s => s.JobVacancy)
            .AsQueryable();

        if (jobVacancyId.HasValue)
            query = query.Where(s => s.JobVacancyId == jobVacancyId.Value);
        if (interviewerId.HasValue)
            query = query.Where(s => s.InterviewerId == interviewerId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InterviewSlotStatus>(status, true, out var s2))
            query = query.Where(s => s.Status == s2);

        var items = await query.OrderBy(s => s.ScheduledAt).ToListAsync();
        return items.Select(MapSlotToResponse).ToList();
    }

    public async Task CancelSlotAsync(Guid slotId)
    {
        var slot = await _context.InterviewSlots.FindAsync(slotId)
            ?? throw new KeyNotFoundException("Interview slot not found");

        if (slot.Status != InterviewSlotStatus.Open)
            throw new InvalidOperationException("Only open slots can be cancelled");

        slot.Status = InterviewSlotStatus.Cancelled;
        slot.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<InterviewResponse> BookSlotAsync(Guid slotId, Guid candidateId)
    {
        var slot = await _context.InterviewSlots.FindAsync(slotId)
            ?? throw new KeyNotFoundException("Interview slot not found");

        if (slot.Status != InterviewSlotStatus.Open)
            throw new InvalidOperationException("This slot is no longer available");

        var candidate = await _context.Candidates.FindAsync(candidateId)
            ?? throw new KeyNotFoundException("Candidate not found");

        if (candidate.JobVacancyId != slot.JobVacancyId)
            throw new InvalidOperationException("This slot is not for the candidate's vacancy");

        // Re-check the interviewer conflict at booking time too (not just when the slot was
        // opened) in case the interviewer picked up another interview in the meantime.
        await EnsureNoConflictAsync(slot.InterviewerId, slot.ScheduledAt, slot.DurationMinutes);

        // Same read-then-write pattern (and the same narrow TOCTOU race window) as
        // EnsureNoConflictAsync above and CreateAsync's conflict check -- this codebase doesn't
        // use transactions or concurrency tokens anywhere else, so two candidates racing to
        // book the exact same slot in the same instant is an accepted, low-frequency edge case
        // (worst case: both see it as booked and staff resolves manually), not a new gap.
        var interview = new Interview
        {
            CandidateId = candidateId,
            InterviewerId = slot.InterviewerId,
            ScheduledAt = slot.ScheduledAt,
            DurationMinutes = slot.DurationMinutes,
            Type = slot.Type,
            Status = InterviewStatus.Scheduled
        };
        _context.Interviews.Add(interview);

        slot.Status = InterviewSlotStatus.Booked;
        slot.BookedByCandidateId = candidateId;
        slot.InterviewId = interview.Id;
        slot.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(interview.Id);
    }
}
