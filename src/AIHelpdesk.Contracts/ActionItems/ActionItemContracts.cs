namespace AIHelpdesk.Contracts.ActionItems;

public record CreateActionItemRequest(
    Guid? MeetingId,
    string Title,
    string? Description,
    Guid AssignedToId,
    DateTime DueDate,
    string Priority); // Low, Medium, High, Urgent

public record UpdateActionItemRequest(
    string Title,
    string? Description,
    Guid AssignedToId,
    DateTime DueDate,
    string Priority);

public record ActionItemResponse(
    Guid Id,
    Guid? MeetingId,
    string? MeetingTitle,
    string Title,
    string? Description,
    Guid AssignedToId,
    string AssignedToName,
    DateTime DueDate,
    string Priority,
    string Status,
    DateTime? CompletedAt,
    DateTime CreatedAt);
