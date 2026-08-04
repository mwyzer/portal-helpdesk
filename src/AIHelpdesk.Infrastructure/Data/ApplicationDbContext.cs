using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingParticipant> MeetingParticipants => Set<MeetingParticipant>();
    public DbSet<MeetingNote> MeetingNotes => Set<MeetingNote>();
    public DbSet<ActionItem> ActionItems => Set<ActionItem>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<DocumentRequest> DocumentRequests => Set<DocumentRequest>();
    public DbSet<GeneratedDocument> GeneratedDocuments => Set<GeneratedDocument>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AIResponse> AIResponses => Set<AIResponse>();
    public DbSet<AIUsageLog> AIUsageLogs => Set<AIUsageLog>();

    // ─────── Phase 2: HR Module ───────
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveApproval> LeaveApprovals => Set<LeaveApproval>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // ─────── Phase 5: Ticketing Module ───────
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();
    public DbSet<TicketSLA> TicketSLAs => Set<TicketSLA>();
    public DbSet<Escalation> Escalations => Set<Escalation>();
    public DbSet<AgentAssignment> AgentAssignments => Set<AgentAssignment>();

    // ─────── Phase 6: Recruitment Module ───────
    public DbSet<JobVacancy> JobVacancies => Set<JobVacancy>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateStageHistory> CandidateStageHistories => Set<CandidateStageHistory>();
    public DbSet<CandidateDocument> CandidateDocuments => Set<CandidateDocument>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();

    // ─────── Phase 8: Candidate Self-Service Portal ───────
    public DbSet<CandidateAccount> CandidateAccounts => Set<CandidateAccount>();
    public DbSet<InterviewSlot> InterviewSlots => Set<InterviewSlot>();
    public DbSet<CandidatePortalRefreshToken> CandidatePortalRefreshTokens => Set<CandidatePortalRefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.NIK).HasMaxLength(50);
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId).OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasQueryFilter(e => e.IsActive);
        });

        builder.Entity<Department>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<Position>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<Permission>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Group).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.Property(e => e.Token).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.User).WithMany(u => u.RefreshTokens).HasForeignKey(e => e.UserId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Many-to-many: Role <-> Permission
        builder.Entity<Permission>()
            .HasMany(p => p.Roles)
            .WithMany(r => r.Permissions)
            .UsingEntity(j => j.ToTable("RolePermissions"));

        // ─────── Phase 3: Secretary Module ───────

        builder.Entity<Meeting>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.MeetingLink).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Notes).HasMaxLength(5000);
            entity.Property(e => e.TranscriptUrl).HasMaxLength(500);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(e => e.Organizer).WithMany().HasForeignKey(e => e.OrganizerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<MeetingParticipant>(entity =>
        {
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.AttendanceStatus).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(e => e.Meeting).WithMany(m => m.Participants).HasForeignKey(e => e.MeetingId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.MeetingId, e.EmployeeId }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<MeetingNote>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Content).HasMaxLength(10000).IsRequired();
            entity.HasOne(e => e.Meeting).WithMany(m => m.MeetingNotes).HasForeignKey(e => e.MeetingId).OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<ActionItem>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(e => e.Meeting).WithMany(m => m.ActionItems).HasForeignKey(e => e.MeetingId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AssignedTo).WithMany().HasForeignKey(e => e.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<DocumentTemplate>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ContentTemplate).HasColumnType("text").IsRequired();
            entity.Property(e => e.Variables).HasMaxLength(5000);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<DocumentRequest>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.ContentDraft).HasColumnType("text");
            entity.Property(e => e.ContentFinal).HasColumnType("text");
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.LetterNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Template).WithMany().HasForeignKey(e => e.TemplateId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<GeneratedDocument>(entity =>
        {
            entity.Property(e => e.FileName).HasMaxLength(300).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FileFormat).HasConversion<string>().HasMaxLength(10);
            entity.HasOne(e => e.DocumentRequest).WithMany(r => r.GeneratedDocuments).HasForeignKey(e => e.DocumentRequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ─────── Phase 4: AI Chat & Knowledge Base ───────

        builder.Entity<KnowledgeDocument>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.FileType).HasMaxLength(10).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<KnowledgeChunk>(entity =>
        {
            entity.Property(e => e.Content).HasColumnType("text").IsRequired();
            entity.Property(e => e.EmbeddingJson).HasColumnType("text").IsRequired();
            entity.HasOne(e => e.Document).WithMany(d => d.Chunks).HasForeignKey(e => e.DocumentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.DocumentId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // pgvector extension (applied via migration SQL)
        builder.HasPostgresExtension("vector");

        builder.Entity<ChatSession>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<ChatMessage>(entity =>
        {
            entity.Property(e => e.Content).HasColumnType("text").IsRequired();
            entity.Property(e => e.Sources).HasColumnType("text");
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(e => e.Session).WithMany(s => s.Messages).HasForeignKey(e => e.SessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<AIResponse>(entity =>
        {
            entity.Property(e => e.ModelUsed).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FeedbackComment).HasMaxLength(1000);
            entity.HasOne(e => e.Message).WithOne(m => m.AIResponse).HasForeignKey<AIResponse>(e => e.MessageId).OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<AIUsageLog>(entity =>
        {
            entity.Property(e => e.Endpoint).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Cost).HasColumnType("decimal(18,6)");
            entity.Property(e => e.Metadata).HasMaxLength(2000);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ─────── Phase 2: HR Module ───────

        builder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.EmployeeNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.EmploymentStatus).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.WorkLocation).HasMaxLength(200);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Manager).WithMany(e => e.Subordinates).HasForeignKey(e => e.ManagerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.EmployeeNo).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<LeaveType>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<LeaveBalance>(entity =>
        {
            entity.Property(e => e.TotalDays).HasColumnType("decimal(5,1)");
            entity.Property(e => e.UsedDays).HasColumnType("decimal(5,1)");
            entity.Property(e => e.PendingDays).HasColumnType("decimal(5,1)");
            entity.HasOne(e => e.Employee).WithMany(e => e.LeaveBalances).HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LeaveType).WithMany().HasForeignKey(e => e.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.Year }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<LeaveRequest>(entity =>
        {
            entity.Property(e => e.Reason).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.AttachmentUrl).HasMaxLength(500);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.HasOne(e => e.Employee).WithMany(e => e.LeaveRequests).HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LeaveType).WithMany().HasForeignKey(e => e.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<LeaveApproval>(entity =>
        {
            entity.Property(e => e.ApproverRole).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.HasOne(e => e.LeaveRequest).WithMany(lr => lr.Approvals).HasForeignKey(e => e.LeaveRequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Approver).WithMany().HasForeignKey(e => e.ApproverId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Body).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ReferenceType).HasMaxLength(100);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ─────── Phase 5: Ticketing Module ───────

        builder.Entity<TicketCategory>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.DefaultPriority).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<Ticket>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(50000);
            entity.Property(e => e.SubCategory).HasMaxLength(200);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.SLAStatus).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(e => e.Category).WithMany(c => c.Tickets).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedTo).WithMany().HasForeignKey(e => e.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedAgent).WithMany().HasForeignKey(e => e.AssignedAgentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SubmittedBy).WithMany().HasForeignKey(e => e.SubmittedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.SLAStatus);
            entity.HasIndex(e => new { e.CategoryId, e.Status });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<TicketComment>(entity =>
        {
            entity.Property(e => e.Content).HasMaxLength(50000).IsRequired();
            entity.HasOne(e => e.Ticket).WithMany(t => t.Comments).HasForeignKey(e => e.TicketId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Author).WithMany().HasForeignKey(e => e.AuthorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<TicketAttachment>(entity =>
        {
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(200).IsRequired();
            entity.HasOne(e => e.Ticket).WithMany(t => t.Attachments).HasForeignKey(e => e.TicketId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.UploadedBy).WithMany().HasForeignKey(e => e.UploadedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<TicketHistory>(entity =>
        {
            entity.Property(e => e.Field).HasMaxLength(100).IsRequired();
            entity.Property(e => e.OldValue).HasMaxLength(500);
            entity.Property(e => e.NewValue).HasMaxLength(500);
            entity.HasOne(e => e.Ticket).WithMany(t => t.History).HasForeignKey(e => e.TicketId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ChangedBy).WithMany().HasForeignKey(e => e.ChangedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.TicketId, e.CreatedAt });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<TicketSLA>(entity =>
        {
            entity.HasOne(e => e.Ticket).WithMany(t => t.SLARecords).HasForeignKey(e => e.TicketId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<Escalation>(entity =>
        {
            entity.Property(e => e.Reason).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(e => e.Ticket).WithMany(t => t.Escalations).HasForeignKey(e => e.TicketId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.EscalatedBy).WithMany().HasForeignKey(e => e.EscalatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedTo).WithMany().HasForeignKey(e => e.AssignedToId).OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<AgentAssignment>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.UserId, e.DepartmentId }).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<JobVacancy>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(20000);
            entity.Property(e => e.Requirements).HasMaxLength(20000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.PostedBy).WithMany().HasForeignKey(e => e.PostedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Status);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<Candidate>(entity =>
        {
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.Stage).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.AISummaryJson).HasColumnType("text");
            entity.Property(e => e.RejectionReason).HasMaxLength(2000);
            entity.HasOne(e => e.JobVacancy).WithMany(v => v.Candidates).HasForeignKey(e => e.JobVacancyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Stage);
            entity.HasIndex(e => new { e.JobVacancyId, e.Stage });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<CandidateStageHistory>(entity =>
        {
            entity.Property(e => e.FromStage).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ToStage).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasOne(e => e.Candidate).WithMany(c => c.StageHistory).HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ChangedBy).WithMany().HasForeignKey(e => e.ChangedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<CandidateDocument>(entity =>
        {
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(200).IsRequired();
            entity.HasOne(e => e.Candidate).WithMany(c => c.Documents).HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.UploadedBy).WithMany().HasForeignKey(e => e.UploadedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ─────── Phase 8: Candidate Self-Service Portal ───────

        builder.Entity<CandidateAccount>(entity =>
        {
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.SetupToken).HasMaxLength(200);
            entity.HasOne(e => e.Candidate).WithOne().HasForeignKey<CandidateAccount>(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.CandidateId).IsUnique();
            entity.HasIndex(e => e.SetupToken);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<InterviewSlot>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(e => e.Interviewer).WithMany().HasForeignKey(e => e.InterviewerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.JobVacancy).WithMany(v => v.InterviewSlots).HasForeignKey(e => e.JobVacancyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.BookedByCandidate).WithMany().HasForeignKey(e => e.BookedByCandidateId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Interview).WithOne().HasForeignKey<InterviewSlot>(e => e.InterviewId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.InterviewerId, e.ScheduledAt });
            entity.HasIndex(e => new { e.JobVacancyId, e.Status });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<CandidatePortalRefreshToken>(entity =>
        {
            entity.Property(e => e.Token).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.Candidate).WithMany().HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<Interview>(entity =>
        {
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Recommendation).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Feedback).HasMaxLength(5000);
            entity.HasOne(e => e.Candidate).WithMany(c => c.Interviews).HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Interviewer).WithMany().HasForeignKey(e => e.InterviewerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => new { e.InterviewerId, e.ScheduledAt });
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<InterviewQuestion>(entity =>
        {
            entity.Property(e => e.Question).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.HasOne(e => e.Interview).WithMany(i => i.Questions).HasForeignKey(e => e.InterviewId).OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Seed default permissions
        SeedPermissions(builder);
    }

    private static void SeedPermissions(ModelBuilder builder)
    {
        var createdDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var permissions = new List<Permission>
        {
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"), Name = "users.read", Group = "Users", Description = "View users", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000002"), Name = "users.create", Group = "Users", Description = "Create users", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000003"), Name = "users.update", Group = "Users", Description = "Update users", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000004"), Name = "users.delete", Group = "Users", Description = "Delete users", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000005"), Name = "roles.read", Group = "Roles", Description = "View roles", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000006"), Name = "roles.create", Group = "Roles", Description = "Create roles", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000007"), Name = "roles.update", Group = "Roles", Description = "Update roles", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000008"), Name = "roles.delete", Group = "Roles", Description = "Delete roles", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000009"), Name = "departments.read", Group = "Departments", Description = "View departments", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000010"), Name = "departments.create", Group = "Departments", Description = "Create departments", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000011"), Name = "departments.update", Group = "Departments", Description = "Update departments", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000012"), Name = "employee.read", Group = "Employee", Description = "View employees", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000013"), Name = "employee.create", Group = "Employee", Description = "Create employees", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000014"), Name = "leave.submit", Group = "Leave", Description = "Submit leave requests", CreatedAt = createdDate},
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000015"), Name = "leave.approve", Group = "Leave", Description = "Approve leave requests", CreatedAt = createdDate},
        };

        builder.Entity<Permission>().HasData(permissions);
    }
}
