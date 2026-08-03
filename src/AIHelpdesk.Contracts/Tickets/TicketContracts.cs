namespace AIHelpdesk.Contracts.Tickets;

// ═══════════════ Ticket Category ═══════════════

public record CreateTicketCategoryRequest(
    string Name,
    string Description,
    string DefaultPriority,
    int SLAHours,
    Guid? DepartmentId);

public record UpdateTicketCategoryRequest(
    string Name,
    string Description,
    string DefaultPriority,
    int SLAHours,
    Guid? DepartmentId);

public record TicketCategoryResponse(
    Guid Id,
    string Name,
    string Description,
    string DefaultPriority,
    int SLAHours,
    Guid? DepartmentId,
    string? DepartmentName,
    DateTime CreatedAt);

// ═══════════════ Ticket ═══════════════

public record CreateTicketRequest(
    Guid CategoryId,
    string Title,
    string Description,
    string? SubCategory,
    string? Priority);

public record UpdateTicketRequest(
    string Title,
    string Description,
    string? SubCategory,
    string? Priority);

public record TicketResponse(
    Guid Id,
    string Title,
    string Description,
    Guid CategoryId,
    string CategoryName,
    string? SubCategory,
    string Priority,
    string Status,
    Guid AssignedToId,
    string AssignedToName,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    Guid SubmittedById,
    string SubmittedByName,
    Guid? DepartmentId,
    string? DepartmentName,
    DateTime? SLADeadline,
    string SLAStatus,
    int CommentCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TicketDetailResponse(
    Guid Id,
    string Title,
    string Description,
    Guid CategoryId,
    string CategoryName,
    string? SubCategory,
    string Priority,
    string Status,
    Guid AssignedToId,
    string AssignedToName,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    Guid SubmittedById,
    string SubmittedByName,
    Guid? DepartmentId,
    string? DepartmentName,
    DateTime? SLADeadline,
    string SLAStatus,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    List<TicketCommentResponse> Comments,
    List<TicketAttachmentResponse> Attachments,
    List<TicketHistoryResponse> History,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ═══════════════ Comments ═══════════════

public record CreateTicketCommentRequest(
    string Content,
    bool IsInternal);

public record TicketCommentResponse(
    Guid Id,
    Guid TicketId,
    Guid AuthorId,
    string AuthorName,
    string Content,
    bool IsInternal,
    DateTime CreatedAt);

// ═══════════════ Attachments ═══════════════

public record TicketAttachmentResponse(
    Guid Id,
    Guid TicketId,
    string FileName,
    long FileSize,
    string ContentType,
    Guid UploadedById,
    string UploadedByName,
    DateTime CreatedAt);

// ═══════════════ History ═══════════════

public record TicketHistoryResponse(
    Guid Id,
    Guid TicketId,
    string Field,
    string? OldValue,
    string? NewValue,
    Guid ChangedById,
    string ChangedByName,
    DateTime CreatedAt);

// ═══════════════ Escalation ═══════════════

public record CreateEscalationRequest(
    string Reason,
    Guid? AssignedToId);

public record EscalationResponse(
    Guid Id,
    Guid TicketId,
    Guid EscalatedById,
    string EscalatedByName,
    Guid? AssignedToId,
    string? AssignedToName,
    string Reason,
    string Status,
    DateTime? ResolvedAt,
    DateTime CreatedAt);

// ═══════════════ Agent ═══════════════

public record CreateAgentAssignmentRequest(
    Guid UserId,
    Guid DepartmentId,
    int MaxTickets);

public record UpdateAgentAssignmentRequest(
    int MaxTickets,
    bool IsActive);

public record AgentAssignmentResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    Guid DepartmentId,
    string DepartmentName,
    bool IsActive,
    int MaxTickets,
    int CurrentLoad,
    DateTime CreatedAt);

// ═══════════════ AI Suggestion ═══════════════

public record TicketAISuggestionResponse(
    string SuggestedCategory,
    Guid? CategoryId,
    string SuggestedPriority,
    string Reason,
    double Confidence);

// ═══════════════ Stats ═══════════════

public record TicketStatsResponse(
    int Total,
    int Open,
    int InProgress,
    int Resolved,
    int Closed,
    int Breached,
    double AverageResolutionHours);

public record TicketSLAReportResponse(
    Guid TicketId,
    string Title,
    string CategoryName,
    string Priority,
    DateTime? SLADeadline,
    string SLAStatus,
    DateTime CreatedAt);

public record TicketQueueResponse(
    Guid Id,
    string Title,
    string CategoryName,
    string Priority,
    string Status,
    DateTime? SLADeadline,
    string SLAStatus,
    string SubmittedByName,
    DateTime CreatedAt);
