using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Domain.Common;
using Bogus;
using Microsoft.AspNetCore.Identity;

namespace AIHelpdesk.Tests;

public static class TestDataFactory
{
    private static readonly Faker Faker = new();

    // ── Phase 1 factories ──

    public static ApplicationUser CreateUser(
        string email = "test@example.com",
        string fullName = "Test User",
        string? nik = "NIK-001",
        bool isActive = true)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            NIK = nik,
            IsActive = isActive,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public static ApplicationRole CreateRole(string name = "Test Role", string? description = "A test role")
    {
        return new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public static Department CreateDepartment(string name = "IT", string code = "IT")
    {
        return new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static Position CreatePosition(Guid departmentId, string name = "Developer")
    {
        return new Position
        {
            Id = Guid.NewGuid(),
            Name = name,
            DepartmentId = departmentId,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static Permission CreatePermission(string name = "users.read", string group = "Users")
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            Group = group,
            Description = $"Permission to {name}",
        };
    }

    public static RefreshToken CreateRefreshToken(Guid userId, bool isRevoked = false)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = isRevoked,
        };
    }

    // ── Phase 2 factories ──

    public static Employee CreateEmployee(
        Guid? userId = null,
        string employeeNo = "EMP-001",
        string fullName = "John Doe",
        string email = "john@example.com",
        string? phone = "+62812345678",
        Guid? departmentId = null,
        Guid? positionId = null,
        Guid? managerId = null,
        EmploymentStatus employmentStatus = EmploymentStatus.Active,
        string? workLocation = "Jakarta")
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmployeeNo = employeeNo,
            FullName = fullName,
            Email = email,
            Phone = phone,
            JoinDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
            DepartmentId = departmentId,
            PositionId = positionId,
            ManagerId = managerId,
            EmploymentStatus = employmentStatus,
            WorkLocation = workLocation,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static LeaveType CreateLeaveType(
        string name = "Annual Leave",
        string code = "AL",
        int daysPerYear = 12,
        bool isPaid = true,
        int minServiceMonths = 0,
        bool requiresAttachment = false,
        bool skipManagerApproval = false)
    {
        return new LeaveType
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            DaysPerYear = daysPerYear,
            IsPaid = isPaid,
            IsActive = true,
            MinServiceMonths = minServiceMonths,
            RequiresAttachment = requiresAttachment,
            SkipManagerApproval = skipManagerApproval,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static LeaveBalance CreateLeaveBalance(
        Guid employeeId,
        Guid leaveTypeId,
        int year = 2026,
        int totalDays = 12,
        int usedDays = 0,
        int pendingDays = 0)
    {
        return new LeaveBalance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            TotalDays = totalDays,
            UsedDays = usedDays,
            PendingDays = pendingDays,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static LeaveRequest CreateLeaveRequest(
        Guid employeeId,
        Guid leaveTypeId,
        string reason = "Personal leave",
        LeaveRequestStatus status = LeaveRequestStatus.Draft,
        int days = 2)
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        return new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate,
            EndDate = startDate.AddDays(days - 1),
            TotalDays = days,
            Reason = reason,
            Status = status,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static LeaveApproval CreateLeaveApproval(
        Guid leaveRequestId,
        Guid approverId,
        string approverRole = "Manager",
        ApprovalStatus status = ApprovalStatus.Approved,
        string? note = null)
    {
        return new LeaveApproval
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = leaveRequestId,
            ApproverId = approverId,
            ApproverRole = approverRole,
            Status = status,
            Note = note,
            ApprovedAt = status != ApprovalStatus.Pending ? DateTime.UtcNow : null,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ── Phase 3 factories ──

    public static Meeting CreateMeeting(
        Guid organizerId,
        string title = "Team Sync",
        DateTime? date = null,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        string? location = "Room A",
        MeetingStatus status = MeetingStatus.Scheduled)
    {
        return new Meeting
        {
            Id = Guid.NewGuid(),
            Title = title,
            Date = date ?? DateTime.UtcNow.Date,
            StartTime = startTime ?? new TimeSpan(9, 0, 0),
            EndTime = endTime ?? new TimeSpan(10, 0, 0),
            OrganizerId = organizerId,
            Location = location,
            Status = status,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static MeetingParticipant CreateMeetingParticipant(
        Guid meetingId,
        Guid employeeId,
        ParticipantRole role = ParticipantRole.Attendee,
        bool isRequired = true,
        AttendanceStatus attendanceStatus = AttendanceStatus.Pending)
    {
        return new MeetingParticipant
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            EmployeeId = employeeId,
            Role = role,
            IsRequired = isRequired,
            AttendanceStatus = attendanceStatus,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static MeetingNote CreateMeetingNote(
        Guid meetingId,
        string title = "Discussion Points",
        string content = "Content here",
        bool isAISummary = false,
        Guid? createdBy = null)
    {
        return new MeetingNote
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            Title = title,
            Content = content,
            IsAISummary = isAISummary,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static ActionItem CreateActionItem(
        Guid meetingId,
        Guid assignedToId,
        string title = "Follow up task",
        string? description = "Task description",
        DateTime? dueDate = null,
        ActionItemPriority priority = ActionItemPriority.Medium,
        ActionItemStatus status = ActionItemStatus.Open)
    {
        return new ActionItem
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            Title = title,
            Description = description,
            AssignedToId = assignedToId,
            DueDate = dueDate ?? DateTime.UtcNow.AddDays(3),
            Priority = priority,
            Status = status,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static DocumentTemplate CreateDocumentTemplate(
        string name = "Leave Letter",
        string code = "LL",
        string category = "Leave",
        string contentTemplate = "Dear Manager,\nI, {employee_name}, would like to request leave...",
        string variables = "employee_name,date,reason",
        bool isActive = true)
    {
        return new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            Category = category,
            ContentTemplate = contentTemplate,
            Variables = variables,
            IsActive = isActive,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static DocumentRequest CreateDocumentRequest(
        Guid employeeId,
        Guid templateId,
        string title = "Leave Request",
        DocumentRequestStatus status = DocumentRequestStatus.Draft)
    {
        return new DocumentRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TemplateId = templateId,
            Title = title,
            Status = status,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static Notification CreateNotification(
        Guid userId,
        string title = "Test Notification",
        string body = "Test body",
        NotificationType type = NotificationType.Info,
        bool isRead = false,
        string? referenceType = null,
        Guid? referenceId = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IsRead = isRead,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ── Phase 4 factories ──

    public static ChatSession CreateChatSession(
        Guid? userId = null,
        string title = "Test Chat",
        ChatSessionStatus status = ChatSessionStatus.Active)
    {
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Title = title,
            Status = status,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public static ChatMessage CreateChatMessage(
        Guid sessionId,
        ChatMessageRole role = ChatMessageRole.User,
        string content = "Hello",
        string? sources = null)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Content = content,
            Sources = sources,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static AIResponse CreateAIResponse(
        Guid messageId,
        string modelUsed = "gpt-4o-mini",
        int promptTokens = 100,
        int completionTokens = 50,
        int totalTokens = 150,
        long latencyMs = 500)
    {
        return new AIResponse
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            ModelUsed = modelUsed,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = totalTokens,
            LatencyMs = latencyMs,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static AIUsageLog CreateAIUsageLog(
        Guid? userId = null,
        string endpoint = "chat/completions",
        int tokensUsed = 150,
        decimal cost = 0.00001m)
    {
        return new AIUsageLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Endpoint = endpoint,
            TokensUsed = tokensUsed,
            Cost = cost,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static KnowledgeDocument CreateKnowledgeDocument(
        string title = "Employee Handbook",
        string fileName = "handbook.pdf",
        string fileType = ".pdf",
        KnowledgeDocumentStatus status = KnowledgeDocumentStatus.Ready,
        int? chunkCount = null,
        string? errorMessage = null)
    {
        return new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Title = title,
            FileName = fileName,
            FilePath = $"/uploads/knowledge/{Guid.NewGuid()}.pdf",
            FileType = fileType,
            ContentType = "application/pdf",
            FileSize = 1024,
            Status = status,
            ChunkCount = chunkCount,
            ErrorMessage = errorMessage,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public static KnowledgeChunk CreateKnowledgeChunk(
        Guid documentId,
        string content = "This is a test chunk of knowledge content.",
        int chunkIndex = 0,
        string embeddingJson = "[0.1, 0.2, 0.3]")
    {
        return new KnowledgeChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Content = content,
            ChunkIndex = chunkIndex,
            EmbeddingJson = embeddingJson,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
