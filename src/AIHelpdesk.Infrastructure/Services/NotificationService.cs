using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Notifications;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationListResponse> GetNotificationsAsync(Guid userId, int page, int pageSize, bool? isRead)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        var totalCount = await query.CountAsync();
        var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationResponse(
                n.Id, n.Title, n.Body, n.Type.ToString(),
                n.ReferenceType, n.ReferenceId, n.IsRead,
                n.ReadAt, n.CreatedAt))
            .ToListAsync();

        return new NotificationListResponse(items, totalCount, unreadCount);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(Guid id, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
            throw new KeyNotFoundException("Notification not found");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task CreateNotificationAsync(Guid userId, string title, string body, string type, string? referenceType, Guid? referenceId)
    {
        if (!Enum.TryParse<Domain.Common.NotificationType>(type, out var notificationType))
            notificationType = Domain.Common.NotificationType.Info;

        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = notificationType,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }
}
