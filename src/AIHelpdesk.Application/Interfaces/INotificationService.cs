using AIHelpdesk.Contracts.Notifications;

namespace AIHelpdesk.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationListResponse> GetNotificationsAsync(Guid userId, int page, int pageSize, bool? isRead);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid id, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
    Task CreateNotificationAsync(Guid userId, string title, string body, string type, string? referenceType, Guid? referenceId);
}
