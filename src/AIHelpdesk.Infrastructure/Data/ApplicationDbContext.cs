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
