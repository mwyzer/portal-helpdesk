using System.Text.Json;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Tickets;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIHelpdesk.Infrastructure.Services;

public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAIService _ai;
    private readonly ILogger<TicketService> _logger;

    private const string AISuggestionSystemPrompt =
        "You are a helpdesk ticket triage assistant. Given a ticket title and description, " +
        "choose the single best-fitting category from this list: {0}. " +
        "Also suggest a priority (Low, Normal, High, or Urgent). " +
        "Respond with ONLY a JSON object, no markdown, in this exact shape: " +
        "{{\"category\":\"<one of the listed category names>\",\"priority\":\"<Low|Normal|High|Urgent>\"," +
        "\"reason\":\"<one sentence why>\",\"confidence\":<number between 0 and 1>}}";

    public TicketService(ApplicationDbContext context, IConfiguration configuration, IAIService ai, ILogger<TicketService> logger)
    {
        _context = context;
        _configuration = configuration;
        _ai = ai;
        _logger = logger;
    }

    private TicketResponse MapToResponse(Ticket t) => new(
        t.Id, t.Title, t.Description, t.CategoryId, t.Category.Name,
        t.SubCategory, t.Priority.ToString(), t.Status.ToString(),
        t.AssignedToId, t.AssignedTo.FullName, t.AssignedAgentId,
        t.AssignedAgent != null ? t.AssignedAgent.FullName : null,
        t.SubmittedById, t.SubmittedBy.FullName,
        t.DepartmentId, t.Department != null ? t.Department.Name : null,
        t.SLADeadline, t.SLAStatus.ToString(), t.Comments.Count,
        t.CreatedAt, t.UpdatedAt);

    public async Task<PagedResult<TicketResponse>> GetMyTicketsAsync(Guid userId, int page, int pageSize, string? status, string? priority)
    {
        var query = _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.AssignedTo)
            .Include(t => t.AssignedAgent)
            .Include(t => t.SubmittedBy)
            .Include(t => t.Department)
            .Include(t => t.Comments)
            .Where(t => t.SubmittedById == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Common.TicketStatus>(status, true, out var s))
            query = query.Where(t => t.Status == s);
        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<Domain.Common.TicketPriority>(priority, true, out var p))
            query = query.Where(t => t.Priority == p);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TicketResponse>(items.Select(MapToResponse).ToList(), totalCount, page, pageSize);
    }

    public async Task<PagedResult<TicketResponse>> GetAssignedTicketsAsync(Guid agentId, int page, int pageSize, string? status)
    {
        var query = _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.AssignedTo)
            .Include(t => t.AssignedAgent)
            .Include(t => t.SubmittedBy)
            .Include(t => t.Department)
            .Include(t => t.Comments)
            .Where(t => t.AssignedAgentId == agentId || t.AssignedToId == agentId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Common.TicketStatus>(status, true, out var s))
            query = query.Where(t => t.Status == s);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.SLADeadline)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TicketResponse>(items.Select(MapToResponse).ToList(), totalCount, page, pageSize);
    }

    public async Task<PagedResult<TicketResponse>> GetDepartmentTicketsAsync(Guid departmentId, int page, int pageSize, string? status)
    {
        var query = _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.AssignedTo)
            .Include(t => t.AssignedAgent)
            .Include(t => t.SubmittedBy)
            .Include(t => t.Department)
            .Include(t => t.Comments)
            .Where(t => t.DepartmentId == departmentId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Common.TicketStatus>(status, true, out var s))
            query = query.Where(t => t.Status == s);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.SLADeadline)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TicketResponse>(items.Select(MapToResponse).ToList(), totalCount, page, pageSize);
    }

    public async Task<TicketDetailResponse> GetByIdAsync(Guid id)
    {
        var t = await _context.Tickets
            .Include(x => x.Category)
            .Include(x => x.AssignedTo)
            .Include(x => x.AssignedAgent)
            .Include(x => x.SubmittedBy)
            .Include(x => x.Department)
            .Include(x => x.Comments).ThenInclude(c => c.Author)
            .Include(x => x.Attachments).ThenInclude(a => a.UploadedBy)
            .Include(x => x.History).ThenInclude(h => h.ChangedBy)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Ticket not found");

        return new TicketDetailResponse(
            t.Id, t.Title, t.Description, t.CategoryId, t.Category.Name,
            t.SubCategory, t.Priority.ToString(), t.Status.ToString(),
            t.AssignedToId, t.AssignedTo.FullName, t.AssignedAgentId,
            t.AssignedAgent?.FullName, t.SubmittedById, t.SubmittedBy.FullName,
            t.DepartmentId, t.Department?.Name, t.SLADeadline, t.SLAStatus.ToString(),
            t.ResolvedAt, t.ClosedAt,
            t.Comments.OrderBy(c => c.CreatedAt).Select(c => new TicketCommentResponse(
                c.Id, c.TicketId, c.AuthorId, c.Author.FullName, c.Content, c.IsInternal, c.CreatedAt)).ToList(),
            t.Attachments.Select(a => new TicketAttachmentResponse(
                a.Id, a.TicketId, a.FileName, a.FileSize, a.ContentType,
                a.UploadedById, a.UploadedBy.FullName, a.CreatedAt)).ToList(),
            t.History.OrderByDescending(h => h.CreatedAt).Select(h => new TicketHistoryResponse(
                h.Id, h.TicketId, h.Field, h.OldValue, h.NewValue,
                h.ChangedById, h.ChangedBy.FullName, h.CreatedAt)).ToList(),
            t.CreatedAt, t.UpdatedAt);
    }

    public async Task<TicketDetailResponse> CreateAsync(Guid userId, CreateTicketRequest request)
    {
        var category = await _context.TicketCategories.FindAsync(request.CategoryId)
            ?? throw new KeyNotFoundException("Category not found");

        var priority = !string.IsNullOrWhiteSpace(request.Priority) &&
            Enum.TryParse<Domain.Common.TicketPriority>(request.Priority, true, out var p) ? p : category.DefaultPriority;

        var slaDeadline = DateTime.UtcNow.AddHours(category.SLAHours);

        var ticket = new Ticket
        {
            Title = request.Title,
            Description = request.Description,
            CategoryId = request.CategoryId,
            SubCategory = request.SubCategory,
            Priority = priority,
            Status = Domain.Common.TicketStatus.Open,
            SubmittedById = userId,
            AssignedToId = userId,
            DepartmentId = category.DepartmentId,
            SLADeadline = slaDeadline,
            SLAStatus = Domain.Common.SLAStatus.OnTrack
        };

        _context.Tickets.Add(ticket);

        // Add creation history
        _context.TicketHistories.Add(new TicketHistory
        {
            TicketId = ticket.Id,
            Field = "Created",
            OldValue = null,
            NewValue = "Ticket created",
            ChangedById = userId
        });

        await _context.SaveChangesAsync();

        return await GetByIdAsync(ticket.Id);
    }

    public async Task<TicketDetailResponse> UpdateAsync(Guid id, UpdateTicketRequest request)
    {
        var ticket = await _context.Tickets.FindAsync(id)
            ?? throw new KeyNotFoundException("Ticket not found");

        ticket.Title = request.Title;
        ticket.Description = request.Description;
        ticket.SubCategory = request.SubCategory;
        if (!string.IsNullOrWhiteSpace(request.Priority) &&
            Enum.TryParse<Domain.Common.TicketPriority>(request.Priority, true, out var pri))
            ticket.Priority = pri;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task UpdateStatusAsync(Guid id, string status)
    {
        var ticket = await _context.Tickets.FindAsync(id)
            ?? throw new KeyNotFoundException("Ticket not found");

        if (!Enum.TryParse<Domain.Common.TicketStatus>(status, true, out var newStatus))
            throw new ArgumentException($"Invalid status: {status}");

        var oldStatus = ticket.Status.ToString();
        ticket.Status = newStatus;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (newStatus == Domain.Common.TicketStatus.Resolved)
            ticket.ResolvedAt = DateTime.UtcNow;
        if (newStatus == Domain.Common.TicketStatus.Closed)
            ticket.ClosedAt = DateTime.UtcNow;

        _context.TicketHistories.Add(new TicketHistory
        {
            TicketId = id,
            Field = "Status",
            OldValue = oldStatus,
            NewValue = status,
            ChangedById = ticket.AssignedToId
        });

        await _context.SaveChangesAsync();
    }

    public async Task<TicketDetailResponse> AssignAgentAsync(Guid id, Guid agentId)
    {
        var ticket = await _context.Tickets.FindAsync(id)
            ?? throw new KeyNotFoundException("Ticket not found");

        var oldAgent = ticket.AssignedAgentId?.ToString() ?? "none";
        ticket.AssignedAgentId = agentId;
        ticket.AssignedToId = agentId;
        if (ticket.Status == Domain.Common.TicketStatus.Open)
            ticket.Status = Domain.Common.TicketStatus.Assigned;
        ticket.UpdatedAt = DateTime.UtcNow;

        // Update agent load
        var assignment = await _context.AgentAssignments
            .FirstOrDefaultAsync(a => a.UserId == agentId && a.IsActive);
        if (assignment != null)
            assignment.CurrentLoad++;

        _context.TicketHistories.Add(new TicketHistory
        {
            TicketId = id,
            Field = "AssignedAgent",
            OldValue = oldAgent,
            NewValue = agentId.ToString(),
            ChangedById = ticket.SubmittedById
        });

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<TicketDetailResponse> AddCommentAsync(Guid id, Guid userId, CreateTicketCommentRequest request)
    {
        var ticket = await _context.Tickets.FindAsync(id)
            ?? throw new KeyNotFoundException("Ticket not found");

        var comment = new TicketComment
        {
            TicketId = id,
            AuthorId = userId,
            Content = request.Content,
            IsInternal = request.IsInternal
        };

        _context.TicketComments.Add(comment);
        ticket.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<TicketDetailResponse> ResolveAsync(Guid id, Guid userId)
    {
        var ticket = await _context.Tickets.FindAsync(id)
            ?? throw new KeyNotFoundException("Ticket not found");

        ticket.Status = Domain.Common.TicketStatus.Resolved;
        ticket.ResolvedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        _context.TicketHistories.Add(new TicketHistory
        {
            TicketId = id,
            Field = "Status",
            OldValue = "InProgress",
            NewValue = "Resolved",
            ChangedById = userId
        });

        // Decrement agent load
        if (ticket.AssignedAgentId.HasValue)
        {
            var assignment = await _context.AgentAssignments
                .FirstOrDefaultAsync(a => a.UserId == ticket.AssignedAgentId.Value);
            if (assignment != null && assignment.CurrentLoad > 0)
                assignment.CurrentLoad--;
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<TicketDetailResponse> CloseAsync(Guid id, Guid userId)
    {
        var ticket = await _context.Tickets.FindAsync(id)
            ?? throw new KeyNotFoundException("Ticket not found");

        ticket.Status = Domain.Common.TicketStatus.Closed;
        ticket.ClosedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        _context.TicketHistories.Add(new TicketHistory
        {
            TicketId = id,
            Field = "Status",
            OldValue = "Resolved",
            NewValue = "Closed",
            ChangedById = userId
        });

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<TicketDetailResponse> ReopenAsync(Guid id, Guid userId)
    {
        var ticket = await _context.Tickets.FindAsync(id)
            ?? throw new KeyNotFoundException("Ticket not found");

        ticket.Status = Domain.Common.TicketStatus.Reopened;
        ticket.UpdatedAt = DateTime.UtcNow;

        _context.TicketHistories.Add(new TicketHistory
        {
            TicketId = id,
            Field = "Status",
            OldValue = "Resolved",
            NewValue = "Reopened",
            ChangedById = userId
        });

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".gif", ".txt", ".csv", ".zip"
    };
    private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024; // 10 MB — matches [RequestSizeLimit] on the controller action

    public async Task<TicketAttachmentResponse> UploadAttachmentAsync(Guid ticketId, Guid userId, string fileName, string contentType, Stream fileStream)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId)
            ?? throw new KeyNotFoundException("Ticket not found");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedAttachmentExtensions.Contains(extension))
            throw new InvalidOperationException($"File type '{extension}' is not allowed.");

        if (fileStream.CanSeek && fileStream.Length > MaxAttachmentSizeBytes)
            throw new InvalidOperationException("File exceeds the maximum allowed size of 10 MB.");

        var uploadsDir = _configuration.GetValue<string>("Uploads:Path") ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(uploadsDir);

        var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(uploadsDir, safeFileName);

        await using var output = File.Create(filePath);
        await fileStream.CopyToAsync(output);

        var attachment = new TicketAttachment
        {
            TicketId = ticketId,
            FileName = fileName,
            FilePath = filePath,
            FileSize = new FileInfo(filePath).Length,
            ContentType = contentType,
            UploadedById = userId,
        };

        _context.TicketAttachments.Add(attachment);

        _context.TicketHistories.Add(new TicketHistory
        {
            TicketId = ticketId,
            Field = "Attachment",
            OldValue = null,
            NewValue = $"File attached: {fileName}",
            ChangedById = userId,
        });

        await _context.SaveChangesAsync();

        var uploader = await _context.Users.FindAsync(userId);
        return new TicketAttachmentResponse(
            attachment.Id, attachment.TicketId, attachment.FileName,
            attachment.FileSize, attachment.ContentType,
            attachment.UploadedById, uploader?.FullName ?? "Unknown",
            attachment.CreatedAt);
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadAttachmentAsync(Guid ticketId, Guid attachmentId)
    {
        var attachment = await _context.TicketAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == ticketId)
            ?? throw new KeyNotFoundException("Attachment not found");

        if (!File.Exists(attachment.FilePath))
            throw new KeyNotFoundException("Attachment file is missing from storage");

        Stream stream = File.OpenRead(attachment.FilePath);
        return (stream, attachment.ContentType, attachment.FileName);
    }

    public async Task DeleteAttachmentAsync(Guid ticketId, Guid attachmentId, Guid userId)
    {
        var attachment = await _context.TicketAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == ticketId)
            ?? throw new KeyNotFoundException("Attachment not found");

        _context.TicketAttachments.Remove(attachment);
        _context.TicketHistories.Add(new TicketHistory
        {
            TicketId = ticketId,
            Field = "Attachment",
            OldValue = attachment.FileName,
            NewValue = null,
            ChangedById = userId,
        });

        await _context.SaveChangesAsync();

        if (File.Exists(attachment.FilePath))
        {
            try { File.Delete(attachment.FilePath); }
            catch (IOException ex) { _logger.LogWarning(ex, "Failed to delete attachment file {FilePath}", attachment.FilePath); }
        }
    }

    public async Task<TicketStatsResponse> GetStatsAsync(Guid? userId, Guid? departmentId)
    {
        var query = _context.Tickets.AsQueryable();
        if (userId.HasValue)
            query = query.Where(t => t.SubmittedById == userId.Value || t.AssignedToId == userId.Value);
        if (departmentId.HasValue)
            query = query.Where(t => t.DepartmentId == departmentId.Value);

        var total = await query.CountAsync();
        var open = await query.CountAsync(t => t.Status == Domain.Common.TicketStatus.Open);
        var inProgress = await query.CountAsync(t => t.Status == Domain.Common.TicketStatus.Assigned || t.Status == Domain.Common.TicketStatus.InProgress);
        var resolved = await query.CountAsync(t => t.Status == Domain.Common.TicketStatus.Resolved);
        var closed = await query.CountAsync(t => t.Status == Domain.Common.TicketStatus.Closed);
        var breached = await query.CountAsync(t => t.SLAStatus == Domain.Common.SLAStatus.Breached);

        var resolvedTickets = await query
            .Where(t => t.ResolvedAt.HasValue)
            .Select(t => new { t.CreatedAt, ResolvedAt = t.ResolvedAt!.Value })
            .ToListAsync();

        var avgHours = resolvedTickets.Count > 0
            ? resolvedTickets.Average(t => (t.ResolvedAt - t.CreatedAt).TotalHours)
            : 0;

        return new TicketStatsResponse(total, open, inProgress, resolved, closed, breached, Math.Round(avgHours, 1));
    }

    public async Task<PagedResult<TicketQueueResponse>> GetQueueAsync(Guid? departmentId, int page, int pageSize, string? status, string? priority)
    {
        var query = _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.SubmittedBy)
            .AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(t => t.DepartmentId == departmentId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Common.TicketStatus>(status, true, out var s))
            query = query.Where(t => t.Status == s);
        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<Domain.Common.TicketPriority>(priority, true, out var p))
            query = query.Where(t => t.Priority == p);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.SLADeadline)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketQueueResponse(
                t.Id, t.Title, t.Category.Name, t.Priority.ToString(), t.Status.ToString(),
                t.SLADeadline, t.SLAStatus.ToString(), t.SubmittedBy.FullName, t.CreatedAt))
            .ToListAsync();

        return new PagedResult<TicketQueueResponse>(items, totalCount, page, pageSize);
    }

    public async Task<IList<TicketSLAReportResponse>> GetSLAReportAsync(Guid? departmentId)
    {
        var query = _context.Tickets
            .Include(t => t.Category)
            .AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(t => t.DepartmentId == departmentId.Value);

        return await query
            .OrderBy(t => t.SLAStatus)
            .ThenBy(t => t.SLADeadline)
            .Select(t => new TicketSLAReportResponse(
                t.Id, t.Title, t.Category.Name, t.Priority.ToString(),
                t.SLADeadline, t.SLAStatus.ToString(), t.CreatedAt))
            .ToListAsync();
    }

    public async Task<TicketAISuggestionResponse> GetAISuggestionAsync(CreateTicketRequest request)
    {
        var categories = await _context.TicketCategories
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.Id, c.Name, c.DefaultPriority })
            .ToListAsync();

        if (categories.Count == 0)
            return new TicketAISuggestionResponse("General Support", null, "Normal", "No ticket categories are configured.", 0.0);

        var systemPrompt = string.Format(AISuggestionSystemPrompt, string.Join(", ", categories.Select(c => c.Name)));
        var userMessage = $"Title: {request.Title}\nDescription: {request.Description}";

        try
        {
            var raw = await _ai.GenerateChatResponseAsync(systemPrompt, userMessage, new List<(string Role, string Content)>());
            var parsed = ParseAISuggestion(raw);
            if (parsed != null)
            {
                var matched = categories.FirstOrDefault(c => string.Equals(c.Name, parsed.Category, StringComparison.OrdinalIgnoreCase));
                return new TicketAISuggestionResponse(
                    matched?.Name ?? parsed.Category,
                    matched?.Id,
                    parsed.Priority,
                    parsed.Reason,
                    parsed.Confidence);
            }

            _logger.LogWarning("AI ticket suggestion returned unparseable content: {Raw}", raw);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI ticket suggestion call failed; falling back to default category");
        }

        var fallback = categories[0];
        return new TicketAISuggestionResponse(
            fallback.Name, fallback.Id, fallback.DefaultPriority.ToString(),
            "AI suggestion unavailable; defaulted to first configured category.", 0.0);
    }

    private static AISuggestionDto? ParseAISuggestion(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        try
        {
            return JsonSerializer.Deserialize<AISuggestionDto>(
                raw[start..(end + 1)],
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private record AISuggestionDto(string Category, string Priority, string Reason, double Confidence);
}
