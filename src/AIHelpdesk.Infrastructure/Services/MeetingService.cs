using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.ActionItems;
using AIHelpdesk.Contracts.Meetings;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class MeetingService : IMeetingService
{
    private readonly ApplicationDbContext _context;

    public MeetingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<MeetingResponse>> GetMeetingsAsync(int page, int pageSize, DateTime? from, DateTime? to, string? status)
    {
        var query = _context.Meetings.AsQueryable();

        if (from.HasValue)
            query = query.Where(m => m.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(m => m.Date <= to.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MeetingStatus>(status, true, out var parsedStatus))
            query = query.Where(m => m.Status == parsedStatus);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(m => m.Date)
            .ThenBy(m => m.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MeetingResponse(
                m.Id, m.Title, m.Date, m.StartTime, m.EndTime,
                m.Location, m.MeetingLink, m.Description,
                m.Status.ToString(), m.Organizer.FullName,
                m.Participants.Count, m.CreatedAt))
            .ToListAsync();

        return new PagedResult<MeetingResponse>(items, totalCount, page, pageSize);
    }

    public async Task<MeetingDetailResponse> GetMeetingByIdAsync(Guid id)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.Participants).ThenInclude(p => p.Employee)
            .Include(m => m.MeetingNotes)
            .Include(m => m.ActionItems).ThenInclude(a => a.AssignedTo)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting == null)
            throw new KeyNotFoundException("Meeting not found");

        return new MeetingDetailResponse(
            meeting.Id, meeting.Title, meeting.Date, meeting.StartTime, meeting.EndTime,
            meeting.Location, meeting.MeetingLink, meeting.Description,
            meeting.Status.ToString(), meeting.Notes, meeting.TranscriptUrl,
            meeting.OrganizerId, meeting.Organizer.FullName,
            meeting.Participants.Select(p => new MeetingParticipantResponse(
                p.Id, p.EmployeeId, p.Employee.FullName,
                p.Role.ToString(), p.IsRequired, p.AttendanceStatus.ToString())).ToList(),
            meeting.MeetingNotes.Select(n => new MeetingNoteResponse(
                n.Id, n.Title, n.Content, n.IsAISummary,
                n.CreatedBy.HasValue ? "" : "", n.CreatedAt)).ToList(),
            meeting.ActionItems.Select(a => new ActionItemResponse(
                a.Id, a.MeetingId, meeting.Title,
                a.Title, a.Description, a.AssignedToId, a.AssignedTo.FullName,
                a.DueDate, a.Priority.ToString(), a.Status.ToString(),
                a.CompletedAt, a.CreatedAt)).ToList(),
            meeting.CreatedAt, meeting.UpdatedAt);
    }

    public async Task<MeetingResponse> CreateMeetingAsync(Guid organizerId, CreateMeetingRequest request)
    {
        if (request.EndTime <= request.StartTime)
            throw new InvalidOperationException("End time must be after start time");

        var meeting = new Meeting
        {
            Title = request.Title,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            OrganizerId = organizerId,
            Location = request.Location,
            MeetingLink = request.MeetingLink,
            Description = request.Description,
            Status = MeetingStatus.Scheduled
        };

        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();

        return new MeetingResponse(
            meeting.Id, meeting.Title, meeting.Date, meeting.StartTime, meeting.EndTime,
            meeting.Location, meeting.MeetingLink, meeting.Description,
            meeting.Status.ToString(), "", 0, meeting.CreatedAt);
    }

    public async Task<MeetingResponse> UpdateMeetingAsync(Guid id, UpdateMeetingRequest request)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Organizer)
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting == null)
            throw new KeyNotFoundException("Meeting not found");

        if (request.EndTime <= request.StartTime)
            throw new InvalidOperationException("End time must be after start time");

        meeting.Title = request.Title;
        meeting.Date = request.Date;
        meeting.StartTime = request.StartTime;
        meeting.EndTime = request.EndTime;
        meeting.Location = request.Location;
        meeting.MeetingLink = request.MeetingLink;
        meeting.Description = request.Description;
        meeting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new MeetingResponse(
            meeting.Id, meeting.Title, meeting.Date, meeting.StartTime, meeting.EndTime,
            meeting.Location, meeting.MeetingLink, meeting.Description,
            meeting.Status.ToString(), meeting.Organizer.FullName,
            meeting.Participants.Count, meeting.CreatedAt);
    }

    public async Task DeleteMeetingAsync(Guid id)
    {
        var meeting = await _context.Meetings.FindAsync(id);
        if (meeting == null)
            throw new KeyNotFoundException("Meeting not found");

        meeting.IsDeleted = true;
        meeting.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<MeetingParticipantResponse> AddParticipantAsync(Guid meetingId, AddParticipantRequest request)
    {
        var meeting = await _context.Meetings.FindAsync(meetingId);
        if (meeting == null)
            throw new KeyNotFoundException("Meeting not found");

        var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.EmployeeId);
        if (employee == null)
            throw new KeyNotFoundException("Employee not found");

        var participant = new MeetingParticipant
        {
            MeetingId = meetingId,
            EmployeeId = request.EmployeeId,
            Role = Enum.Parse<ParticipantRole>(request.Role),
            IsRequired = request.IsRequired,
            AttendanceStatus = AttendanceStatus.Pending
        };

        _context.MeetingParticipants.Add(participant);
        await _context.SaveChangesAsync();

        return new MeetingParticipantResponse(
            participant.Id, participant.EmployeeId, employee.FullName,
            participant.Role.ToString(), participant.IsRequired, participant.AttendanceStatus.ToString());
    }

    public async Task RemoveParticipantAsync(Guid meetingId, Guid participantId)
    {
        var participant = await _context.MeetingParticipants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.MeetingId == meetingId);

        if (participant == null)
            throw new KeyNotFoundException("Participant not found");

        participant.IsDeleted = true;
        participant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<IList<MeetingResponse>> GetTodayMeetingsAsync(Guid userId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.Meetings
            .Where(m => m.Date.Date == today && m.Status != MeetingStatus.Cancelled)
            .Where(m => m.OrganizerId == userId || m.Participants.Any(p => p.EmployeeId == userId))
            .OrderBy(m => m.StartTime)
            .Select(m => new MeetingResponse(
                m.Id, m.Title, m.Date, m.StartTime, m.EndTime,
                m.Location, m.MeetingLink, m.Description,
                m.Status.ToString(), m.Organizer.FullName,
                m.Participants.Count, m.CreatedAt))
            .ToListAsync();
    }

    public async Task<IList<MeetingResponse>> GetUpcomingMeetingsAsync(Guid userId)
    {
        var today = DateTime.UtcNow.Date;
        var nextWeek = today.AddDays(7);
        return await _context.Meetings
            .Where(m => m.Date >= today && m.Date <= nextWeek && m.Status != MeetingStatus.Cancelled)
            .Where(m => m.OrganizerId == userId || m.Participants.Any(p => p.EmployeeId == userId))
            .OrderBy(m => m.Date).ThenBy(m => m.StartTime)
            .Select(m => new MeetingResponse(
                m.Id, m.Title, m.Date, m.StartTime, m.EndTime,
                m.Location, m.MeetingLink, m.Description,
                m.Status.ToString(), m.Organizer.FullName,
                m.Participants.Count, m.CreatedAt))
            .ToListAsync();
    }

    public async Task<MeetingNoteResponse> AddNoteAsync(Guid meetingId, Guid userId, CreateMeetingNoteRequest request)
    {
        var meeting = await _context.Meetings.FindAsync(meetingId);
        if (meeting == null)
            throw new KeyNotFoundException("Meeting not found");

        var note = new MeetingNote
        {
            MeetingId = meetingId,
            Title = request.Title,
            Content = request.Content,
            IsAISummary = false,
            CreatedBy = userId
        };

        _context.MeetingNotes.Add(note);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        return new MeetingNoteResponse(
            note.Id, note.Title, note.Content, note.IsAISummary,
            user?.FullName ?? "", note.CreatedAt);
    }

    public async Task<MeetingNoteResponse> UpdateNoteAsync(Guid meetingId, Guid noteId, UpdateMeetingNoteRequest request)
    {
        var note = await _context.MeetingNotes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.MeetingId == meetingId);

        if (note == null)
            throw new KeyNotFoundException("Note not found");

        note.Title = request.Title;
        note.Content = request.Content;
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new MeetingNoteResponse(
            note.Id, note.Title, note.Content, note.IsAISummary,
            "", note.CreatedAt);
    }

    public async Task DeleteNoteAsync(Guid meetingId, Guid noteId)
    {
        var note = await _context.MeetingNotes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.MeetingId == meetingId);

        if (note == null)
            throw new KeyNotFoundException("Note not found");

        note.IsDeleted = true;
        note.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<IList<MeetingNoteResponse>> GetNotesAsync(Guid meetingId)
    {
        return await _context.MeetingNotes
            .Where(n => n.MeetingId == meetingId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new MeetingNoteResponse(
                n.Id, n.Title, n.Content, n.IsAISummary,
                n.CreatedBy.HasValue ? "" : "", n.CreatedAt))
            .ToListAsync();
    }

    public async Task<MeetingNoteResponse> GenerateSummaryAsync(Guid meetingId, Guid userId)
    {
        var meeting = await _context.Meetings
            .Include(m => m.MeetingNotes)
            .Include(m => m.Participants).ThenInclude(p => p.Employee)
            .Include(m => m.ActionItems).ThenInclude(a => a.AssignedTo)
            .Include(m => m.Organizer)
            .FirstOrDefaultAsync(m => m.Id == meetingId);

        if (meeting == null)
            throw new KeyNotFoundException("Meeting not found");

        // Collect all notes content
        var notesContent = string.Join("\n\n", meeting.MeetingNotes.Select(n => $"{n.Title}:\n{n.Content}"));

        // Build structured summary from meeting data
        var participantList = string.Join(", ", meeting.Participants.Select(p => p.Employee.FullName));
        var actionItemList = string.Join("\n", meeting.ActionItems.Select(a => $"- {a.Title} (Assigned to: {a.AssignedTo.FullName}, Due: {a.DueDate:yyyy-MM-dd}, Priority: {a.Priority})"));

        var summaryContent = $@"## Meeting Summary: {meeting.Title}

**Date:** {meeting.Date:yyyy-MM-dd}
**Time:** {meeting.StartTime:hh\:mm} - {meeting.EndTime:hh\:mm}
**Location:** {meeting.Location ?? "N/A"}
**Organizer:** {meeting.Organizer.FullName}
**Participants:** {participantList}

### Discussion Notes
{(!string.IsNullOrWhiteSpace(notesContent) ? notesContent : "No detailed notes recorded.")}

### Action Items
{(!string.IsNullOrWhiteSpace(actionItemList) ? actionItemList : "No action items recorded.")}

### Summary
This summary was auto-generated from meeting notes and data.";

        var summary = new MeetingNote
        {
            MeetingId = meetingId,
            Title = $"AI Summary - {meeting.Title}",
            Content = summaryContent,
            IsAISummary = true,
            CreatedBy = userId
        };

        _context.MeetingNotes.Add(summary);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        return new MeetingNoteResponse(
            summary.Id, summary.Title, summary.Content, summary.IsAISummary,
            user?.FullName ?? "", summary.CreatedAt);
    }
}
