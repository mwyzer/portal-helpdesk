namespace AIHelpdesk.Contracts.Notifications;

public record NotificationResponse(
    Guid Id,
    string Title,
    string Body,
    string Type,
    string? ReferenceType,
    Guid? ReferenceId,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt
);

public record NotificationListResponse(
    IList<NotificationResponse> Items,
    int TotalCount,
    int UnreadCount
);
