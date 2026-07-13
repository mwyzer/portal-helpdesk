using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.ActionItems;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class ActionItemService : IActionItemService
{
    private readonly ApplicationDbContext _context;

    public ActionItemService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ActionItemResponse>> GetMyActionItemsAsync(Guid userId, int page, int pageSize, string? status)
    {
        var query = _context.ActionItems
            .Where(a => a.AssignedToId == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ActionItemStatus>(status, true, out var parsedStatus))
            query = query.Where(a => a.Status == parsedStatus);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(a => a.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActionItemResponse(
                a.Id, a.MeetingId, a.Meeting != null ? a.Meeting.Title : null,
                a.Title, a.Description, a.AssignedToId, a.AssignedTo.FullName,
                a.DueDate, a.Priority.ToString(), a.Status.ToString(),
                a.CompletedAt, a.CreatedAt))
            .ToListAsync();

        return new PagedResult<ActionItemResponse>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResult<ActionItemResponse>> GetTeamActionItemsAsync(Guid managerId, int page, int pageSize)
    {
        var query = _context.ActionItems
            .Where(a => a.AssignedTo.Department != null)
            .OrderBy(a => a.DueDate);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActionItemResponse(
                a.Id, a.MeetingId, a.Meeting != null ? a.Meeting.Title : null,
                a.Title, a.Description, a.AssignedToId, a.AssignedTo.FullName,
                a.DueDate, a.Priority.ToString(), a.Status.ToString(),
                a.CompletedAt, a.CreatedAt))
            .ToListAsync();

        return new PagedResult<ActionItemResponse>(items, totalCount, page, pageSize);
    }

    public async Task<IList<ActionItemResponse>> GetOverdueActionItemsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return await _context.ActionItems
            .Where(a => a.AssignedToId == userId && a.DueDate < now && a.Status != ActionItemStatus.Completed && a.Status != ActionItemStatus.Cancelled)
            .OrderBy(a => a.DueDate)
            .Select(a => new ActionItemResponse(
                a.Id, a.MeetingId, a.Meeting != null ? a.Meeting.Title : null,
                a.Title, a.Description, a.AssignedToId, a.AssignedTo.FullName,
                a.DueDate, a.Priority.ToString(), a.Status.ToString(),
                a.CompletedAt, a.CreatedAt))
            .ToListAsync();
    }

    public async Task<ActionItemResponse> GetActionItemByIdAsync(Guid id)
    {
        var item = await _context.ActionItems
            .Include(a => a.AssignedTo)
            .Include(a => a.Meeting)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (item == null)
            throw new KeyNotFoundException("Action item not found");

        return new ActionItemResponse(
            item.Id, item.MeetingId, item.Meeting?.Title,
            item.Title, item.Description, item.AssignedToId, item.AssignedTo.FullName,
            item.DueDate, item.Priority.ToString(), item.Status.ToString(),
            item.CompletedAt, item.CreatedAt);
    }

    public async Task<ActionItemResponse> CreateActionItemAsync(CreateActionItemRequest request)
    {
        var item = new ActionItem
        {
            MeetingId = request.MeetingId,
            Title = request.Title,
            Description = request.Description,
            AssignedToId = request.AssignedToId,
            DueDate = request.DueDate,
            Priority = Enum.TryParse<ActionItemPriority>(request.Priority, true, out var priority) ? priority : ActionItemPriority.Medium,
            Status = ActionItemStatus.Open
        };

        _context.ActionItems.Add(item);
        await _context.SaveChangesAsync();

        var assignee = await _context.Users.FindAsync(request.AssignedToId);
        return new ActionItemResponse(
            item.Id, item.MeetingId, null,
            item.Title, item.Description, item.AssignedToId, assignee?.FullName ?? "",
            item.DueDate, item.Priority.ToString(), item.Status.ToString(),
            item.CompletedAt, item.CreatedAt);
    }

    public async Task<ActionItemResponse> UpdateActionItemAsync(Guid id, UpdateActionItemRequest request)
    {
        var item = await _context.ActionItems
            .Include(a => a.AssignedTo)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (item == null)
            throw new KeyNotFoundException("Action item not found");

        item.Title = request.Title;
        item.Description = request.Description;
        item.AssignedToId = request.AssignedToId;
        item.DueDate = request.DueDate;
        item.Priority = Enum.TryParse<ActionItemPriority>(request.Priority, true, out var up) ? up : ActionItemPriority.Medium;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ActionItemResponse(
            item.Id, item.MeetingId, null,
            item.Title, item.Description, item.AssignedToId, item.AssignedTo.FullName,
            item.DueDate, item.Priority.ToString(), item.Status.ToString(),
            item.CompletedAt, item.CreatedAt);
    }

    public async Task<ActionItemResponse> CompleteActionItemAsync(Guid id, Guid userId)
    {
        var item = await _context.ActionItems
            .Include(a => a.AssignedTo)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (item == null)
            throw new KeyNotFoundException("Action item not found");

        if (item.AssignedToId != userId)
            throw new UnauthorizedAccessException("Only the assignee can complete this action item");

        item.Status = ActionItemStatus.Completed;
        item.CompletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ActionItemResponse(
            item.Id, item.MeetingId, null,
            item.Title, item.Description, item.AssignedToId, item.AssignedTo.FullName,
            item.DueDate, item.Priority.ToString(), item.Status.ToString(),
            item.CompletedAt, item.CreatedAt);
    }

    public async Task<ActionItemResponse> CancelActionItemAsync(Guid id)
    {
        var item = await _context.ActionItems
            .Include(a => a.AssignedTo)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (item == null)
            throw new KeyNotFoundException("Action item not found");

        item.Status = ActionItemStatus.Cancelled;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ActionItemResponse(
            item.Id, item.MeetingId, null,
            item.Title, item.Description, item.AssignedToId, item.AssignedTo.FullName,
            item.DueDate, item.Priority.ToString(), item.Status.ToString(),
            item.CompletedAt, item.CreatedAt);
    }
}
