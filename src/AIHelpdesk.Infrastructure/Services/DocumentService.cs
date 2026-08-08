using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Documents;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AIHelpdesk.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly string _generatedDocsPath;

    public DocumentService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _generatedDocsPath = configuration["Documents:GeneratedPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads", "documents");
        Directory.CreateDirectory(_generatedDocsPath);
    }

    // ─────────── Templates ───────────

    public async Task<IList<DocumentTemplateResponse>> GetTemplatesAsync(string? category)
    {
        var query = _context.DocumentTemplates.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);

        return await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .Select(t => new DocumentTemplateResponse(
                t.Id, t.Name, t.Code, t.Category,
                t.ContentTemplate, t.Variables, t.IsActive, t.CreatedAt))
            .ToListAsync();
    }

    public async Task<DocumentTemplateResponse> GetTemplateByIdAsync(Guid id)
    {
        var template = await _context.DocumentTemplates.FindAsync(id);
        if (template == null)
            throw new KeyNotFoundException("Template not found");

        return new DocumentTemplateResponse(
            template.Id, template.Name, template.Code, template.Category,
            template.ContentTemplate, template.Variables, template.IsActive, template.CreatedAt);
    }

    public async Task<DocumentTemplateResponse> CreateTemplateAsync(CreateDocumentTemplateRequest request)
    {
        var template = new DocumentTemplate
        {
            Name = request.Name,
            Code = request.Code,
            Category = request.Category,
            ContentTemplate = request.ContentTemplate,
            Variables = request.Variables,
            IsActive = true
        };

        _context.DocumentTemplates.Add(template);
        await _context.SaveChangesAsync();

        return new DocumentTemplateResponse(
            template.Id, template.Name, template.Code, template.Category,
            template.ContentTemplate, template.Variables, template.IsActive, template.CreatedAt);
    }

    public async Task<DocumentTemplateResponse> UpdateTemplateAsync(Guid id, UpdateDocumentTemplateRequest request)
    {
        var template = await _context.DocumentTemplates.FindAsync(id);
        if (template == null)
            throw new KeyNotFoundException("Template not found");

        template.Name = request.Name;
        template.Code = request.Code;
        template.Category = request.Category;
        template.ContentTemplate = request.ContentTemplate;
        template.Variables = request.Variables;
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new DocumentTemplateResponse(
            template.Id, template.Name, template.Code, template.Category,
            template.ContentTemplate, template.Variables, template.IsActive, template.CreatedAt);
    }

    public async Task DeleteTemplateAsync(Guid id)
    {
        var template = await _context.DocumentTemplates.FindAsync(id);
        if (template == null)
            throw new KeyNotFoundException("Template not found");

        template.IsDeleted = true;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ─────────── Document Requests ───────────

    // Generated letters can carry sensitive personal/salary content (e.g. surat keterangan
    // kerja), so a plain Employee may only see/act on their own -- previously `userId` here was
    // accepted but never used, silently returning every employee's document requests to every
    // authenticated caller. Secretary/Manager/HRD/Super Admin (the roles this controller already
    // grants review/generate/approve endpoints to) bypass the restriction.
    //
    // Despite the "Employee"/"EmployeeId" naming (DocumentRequest.Employee is typed
    // ApplicationUser, and CreateDocumentRequestAsync populates EmployeeId straight from the
    // controller's GetUserId()), this module never goes through the Employees table at all --
    // it's a direct ApplicationUser id, so ownership is a plain equality check, not an
    // Employees.UserId lookup like LeaveRequestService/TicketService use.
    private static void EnsureAccess(Guid docRequestEmployeeId, Guid userId, bool isPrivileged)
    {
        if (!isPrivileged && docRequestEmployeeId != userId)
            throw new UnauthorizedAccessException("You do not have access to this document request.");
    }

    public async Task<PagedResult<DocumentRequestResponse>> GetDocumentRequestsAsync(Guid userId, bool isPrivileged, int page, int pageSize, string? status)
    {
        var query = _context.DocumentRequests
            .Include(r => r.Employee)
            .Include(r => r.Template)
            .AsQueryable();

        if (!isPrivileged)
            query = query.Where(r => r.EmployeeId == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DocumentRequestStatus>(status, true, out var parsedStatus))
            query = query.Where(r => r.Status == parsedStatus);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new DocumentRequestResponse(
                r.Id, r.EmployeeId, r.Employee.FullName,
                r.TemplateId, r.Template.Name, r.Title,
                r.ContentDraft, r.ContentFinal, r.Status.ToString(),
                r.LetterNumber, r.Notes, r.RejectionReason,
                r.CreatedAt, r.UpdatedAt))
            .ToListAsync();

        return new PagedResult<DocumentRequestResponse>(items, totalCount, page, pageSize);
    }

    public async Task<DocumentRequestDetailResponse> GetDocumentRequestByIdAsync(Guid id, Guid userId, bool isPrivileged)
    {
        var request = await _context.DocumentRequests
            .Include(r => r.Employee)
            .Include(r => r.Template)
            .Include(r => r.GeneratedDocuments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            throw new KeyNotFoundException("Document request not found");
        EnsureAccess(request.EmployeeId, userId, isPrivileged);

        return new DocumentRequestDetailResponse(
            request.Id, request.EmployeeId, request.Employee.FullName,
            request.TemplateId, request.Template.Name, request.Title,
            request.ContentDraft, request.ContentFinal, request.Status.ToString(),
            request.LetterNumber, request.Notes, request.RejectionReason,
            request.GeneratedDocuments.Select(g => new GeneratedDocumentResponse(
                g.Id, g.FileName, g.FilePath, g.FileFormat.ToString(), g.Version, g.GeneratedAt)).ToList(),
            request.CreatedAt, request.UpdatedAt);
    }

    public async Task<DocumentRequestResponse> CreateDocumentRequestAsync(Guid employeeId, CreateDocumentRequestRequest request)
    {
        var template = await _context.DocumentTemplates.FindAsync(request.TemplateId);
        if (template == null)
            throw new KeyNotFoundException("Template not found");

        var docRequest = new DocumentRequest
        {
            EmployeeId = employeeId,
            TemplateId = request.TemplateId,
            Title = request.Title,
            Notes = request.Notes,
            Status = DocumentRequestStatus.Draft
        };

        _context.DocumentRequests.Add(docRequest);
        await _context.SaveChangesAsync();

        return new DocumentRequestResponse(
            docRequest.Id, docRequest.EmployeeId, "",
            docRequest.TemplateId, template.Name, docRequest.Title,
            docRequest.ContentDraft, docRequest.ContentFinal, docRequest.Status.ToString(),
            docRequest.LetterNumber, docRequest.Notes, docRequest.RejectionReason,
            docRequest.CreatedAt, docRequest.UpdatedAt);
    }

    public async Task<DocumentRequestResponse> UpdateDocumentRequestAsync(Guid id, Guid userId, bool isPrivileged, UpdateDocumentRequestRequest request)
    {
        var docRequest = await _context.DocumentRequests
            .Include(r => r.Employee)
            .Include(r => r.Template)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (docRequest == null)
            throw new KeyNotFoundException("Document request not found");
        EnsureAccess(docRequest.EmployeeId, userId, isPrivileged);

        docRequest.Title = request.Title;
        docRequest.ContentDraft = request.ContentDraft;
        docRequest.Notes = request.Notes;
        docRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new DocumentRequestResponse(
            docRequest.Id, docRequest.EmployeeId, docRequest.Employee.FullName,
            docRequest.TemplateId, docRequest.Template.Name, docRequest.Title,
            docRequest.ContentDraft, docRequest.ContentFinal, docRequest.Status.ToString(),
            docRequest.LetterNumber, docRequest.Notes, docRequest.RejectionReason,
            docRequest.CreatedAt, docRequest.UpdatedAt);
    }

    public async Task<DocumentRequestResponse> GenerateDraftAsync(Guid id)
    {
        var docRequest = await _context.DocumentRequests
            .Include(r => r.Employee)
            .Include(r => r.Template)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (docRequest == null)
            throw new KeyNotFoundException("Document request not found");

        if (docRequest.Status != DocumentRequestStatus.Draft && docRequest.Status != DocumentRequestStatus.Submitted)
            throw new InvalidOperationException("Document request is not in a valid state for draft generation");

        // TODO: Integrate AI draft generation in Phase 4
        // For now, use the template content with basic variable substitution
        var content = docRequest.Template.ContentTemplate
            .Replace("{employee_name}", docRequest.Employee.FullName)
            .Replace("{date}", DateTime.UtcNow.ToString("dd MMMM yyyy"));

        docRequest.ContentDraft = content;
        docRequest.Status = DocumentRequestStatus.AiDraftReady;
        docRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new DocumentRequestResponse(
            docRequest.Id, docRequest.EmployeeId, docRequest.Employee.FullName,
            docRequest.TemplateId, docRequest.Template.Name, docRequest.Title,
            docRequest.ContentDraft, docRequest.ContentFinal, docRequest.Status.ToString(),
            docRequest.LetterNumber, docRequest.Notes, docRequest.RejectionReason,
            docRequest.CreatedAt, docRequest.UpdatedAt);
    }

    public async Task<DocumentRequestResponse> SubmitForReviewAsync(Guid id)
    {
        var docRequest = await _context.DocumentRequests
            .Include(r => r.Employee)
            .Include(r => r.Template)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (docRequest == null)
            throw new KeyNotFoundException("Document request not found");

        if (docRequest.Status != DocumentRequestStatus.AiDraftReady)
            throw new InvalidOperationException("Document must have a draft before submitting for review");

        docRequest.Status = DocumentRequestStatus.Review;
        docRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new DocumentRequestResponse(
            docRequest.Id, docRequest.EmployeeId, docRequest.Employee.FullName,
            docRequest.TemplateId, docRequest.Template.Name, docRequest.Title,
            docRequest.ContentDraft, docRequest.ContentFinal, docRequest.Status.ToString(),
            docRequest.LetterNumber, docRequest.Notes, docRequest.RejectionReason,
            docRequest.CreatedAt, docRequest.UpdatedAt);
    }

    public async Task<DocumentRequestResponse> ApproveDocumentAsync(Guid id, Guid reviewerId)
    {
        var docRequest = await _context.DocumentRequests
            .Include(r => r.Employee)
            .Include(r => r.Template)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (docRequest == null)
            throw new KeyNotFoundException("Document request not found");

        if (docRequest.Status != DocumentRequestStatus.Review)
            throw new InvalidOperationException("Document is not in review status");

        docRequest.Status = DocumentRequestStatus.Approved;
        docRequest.UpdatedAt = DateTime.UtcNow;
        docRequest.UpdatedBy = reviewerId;

        await _context.SaveChangesAsync();

        return new DocumentRequestResponse(
            docRequest.Id, docRequest.EmployeeId, docRequest.Employee.FullName,
            docRequest.TemplateId, docRequest.Template.Name, docRequest.Title,
            docRequest.ContentDraft, docRequest.ContentFinal, docRequest.Status.ToString(),
            docRequest.LetterNumber, docRequest.Notes, docRequest.RejectionReason,
            docRequest.CreatedAt, docRequest.UpdatedAt);
    }

    public async Task<DocumentRequestResponse> RejectDocumentAsync(Guid id, string reason)
    {
        var docRequest = await _context.DocumentRequests
            .Include(r => r.Employee)
            .Include(r => r.Template)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (docRequest == null)
            throw new KeyNotFoundException("Document request not found");

        if (docRequest.Status != DocumentRequestStatus.Review)
            throw new InvalidOperationException("Document is not in review status");

        docRequest.Status = DocumentRequestStatus.Rejected;
        docRequest.RejectionReason = reason;
        docRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new DocumentRequestResponse(
            docRequest.Id, docRequest.EmployeeId, docRequest.Employee.FullName,
            docRequest.TemplateId, docRequest.Template.Name, docRequest.Title,
            docRequest.ContentDraft, docRequest.ContentFinal, docRequest.Status.ToString(),
            docRequest.LetterNumber, docRequest.Notes, docRequest.RejectionReason,
            docRequest.CreatedAt, docRequest.UpdatedAt);
    }

    public async Task<DocumentRequestResponse> GenerateFinalAsync(Guid id)
    {
        var docRequest = await _context.DocumentRequests
            .Include(r => r.Employee)
            .Include(r => r.Template)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (docRequest == null)
            throw new KeyNotFoundException("Document request not found");

        if (docRequest.Status != DocumentRequestStatus.Approved)
            throw new InvalidOperationException("Document must be approved before final generation");

        // Generate letter number (yearly counter)
        var currentYear = DateTime.UtcNow.Year;
        var nextNumber = await _context.DocumentRequests
            .CountAsync(r => r.LetterNumber != null && r.LetterNumber.EndsWith($"/{currentYear}")) + 1;

        docRequest.LetterNumber = $"{nextNumber:D3}/{docRequest.Template.Code}/MGR/{currentYear}";
        docRequest.ContentFinal = docRequest.ContentDraft;
        docRequest.Status = DocumentRequestStatus.Generated;
        docRequest.UpdatedAt = DateTime.UtcNow;

        // LetterNumber contains "/" (e.g. "001/CODE/MGR/2026"), which is not a valid filename
        var safeLetterNumber = string.Join("-", docRequest.LetterNumber.Split('/'));
        var docDir = Path.Combine(_generatedDocsPath, docRequest.Id.ToString());
        Directory.CreateDirectory(docDir);

        var body = docRequest.ContentFinal ?? string.Empty;
        var pdfBytes = LetterDocumentGenerator.GeneratePdf(docRequest.Title, docRequest.LetterNumber, DateTime.UtcNow, body);
        var pdfPath = Path.Combine(docDir, $"{safeLetterNumber}.pdf");
        await File.WriteAllBytesAsync(pdfPath, pdfBytes);

        var docxBytes = LetterDocumentGenerator.GenerateDocx(docRequest.Title, docRequest.LetterNumber, DateTime.UtcNow, body);
        var docxPath = Path.Combine(docDir, $"{safeLetterNumber}.docx");
        await File.WriteAllBytesAsync(docxPath, docxBytes);

        var generatedAt = DateTime.UtcNow;
        _context.GeneratedDocuments.Add(new GeneratedDocument
        {
            DocumentRequestId = docRequest.Id,
            FileName = $"{safeLetterNumber}.pdf",
            FilePath = pdfPath,
            FileFormat = Domain.Common.DocumentFormat.PDF,
            Version = 1,
            GeneratedAt = generatedAt
        });
        _context.GeneratedDocuments.Add(new GeneratedDocument
        {
            DocumentRequestId = docRequest.Id,
            FileName = $"{safeLetterNumber}.docx",
            FilePath = docxPath,
            FileFormat = Domain.Common.DocumentFormat.DOCX,
            Version = 1,
            GeneratedAt = generatedAt
        });

        await _context.SaveChangesAsync();

        return new DocumentRequestResponse(
            docRequest.Id, docRequest.EmployeeId, docRequest.Employee.FullName,
            docRequest.TemplateId, docRequest.Template.Name, docRequest.Title,
            docRequest.ContentDraft, docRequest.ContentFinal, docRequest.Status.ToString(),
            docRequest.LetterNumber, docRequest.Notes, docRequest.RejectionReason,
            docRequest.CreatedAt, docRequest.UpdatedAt);
    }

    public async Task<(byte[] FileContents, string FileName, string ContentType)> DownloadDocumentAsync(Guid id, Guid userId, bool isPrivileged, string? format = null)
    {
        var docRequest = await _context.DocumentRequests
            .Include(r => r.GeneratedDocuments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (docRequest == null)
            throw new KeyNotFoundException("Document request not found");
        EnsureAccess(docRequest.EmployeeId, userId, isPrivileged);

        var wantsDocx = string.Equals(format, "docx", StringComparison.OrdinalIgnoreCase);
        var targetFormat = wantsDocx ? Domain.Common.DocumentFormat.DOCX : Domain.Common.DocumentFormat.PDF;

        var doc = docRequest.GeneratedDocuments
            .Where(d => d.FileFormat == targetFormat)
            .OrderByDescending(d => d.Version)
            .FirstOrDefault()
            ?? docRequest.GeneratedDocuments.OrderByDescending(d => d.Version).FirstOrDefault();

        if (doc == null)
            throw new KeyNotFoundException("No generated document found");

        if (!File.Exists(doc.FilePath))
            throw new KeyNotFoundException("Generated document file is missing from storage");

        var content = await File.ReadAllBytesAsync(doc.FilePath);
        var contentType = doc.FileFormat == Domain.Common.DocumentFormat.DOCX
            ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            : "application/pdf";
        return (content, doc.FileName, contentType);
    }
}
