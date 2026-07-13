using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Documents;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;

    public DocumentService(ApplicationDbContext context)
    {
        _context = context;
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

    public async Task<PagedResult<DocumentRequestResponse>> GetDocumentRequestsAsync(Guid userId, int page, int pageSize, string? status)
    {
        var query = _context.DocumentRequests
            .Include(r => r.Employee)
            .Include(r => r.Template)
            .AsQueryable();

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

    public async Task<DocumentRequestDetailResponse> GetDocumentRequestByIdAsync(Guid id)
    {
        var request = await _context.DocumentRequests
            .Include(r => r.Employee)
            .Include(r => r.Template)
            .Include(r => r.GeneratedDocuments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            throw new KeyNotFoundException("Document request not found");

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

    public async Task<DocumentRequestResponse> UpdateDocumentRequestAsync(Guid id, UpdateDocumentRequestRequest request)
    {
        var docRequest = await _context.DocumentRequests
            .Include(r => r.Employee)
            .Include(r => r.Template)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (docRequest == null)
            throw new KeyNotFoundException("Document request not found");

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

        // Create a generated document record
        var genDoc = new GeneratedDocument
        {
            DocumentRequestId = docRequest.Id,
            FileName = $"{docRequest.LetterNumber}.pdf",
            FilePath = $"/documents/{docRequest.Id}/{docRequest.LetterNumber}.pdf",
            FileFormat = DocumentFormat.PDF,
            Version = 1,
            GeneratedAt = DateTime.UtcNow
        };

        _context.GeneratedDocuments.Add(genDoc);
        await _context.SaveChangesAsync();

        return new DocumentRequestResponse(
            docRequest.Id, docRequest.EmployeeId, docRequest.Employee.FullName,
            docRequest.TemplateId, docRequest.Template.Name, docRequest.Title,
            docRequest.ContentDraft, docRequest.ContentFinal, docRequest.Status.ToString(),
            docRequest.LetterNumber, docRequest.Notes, docRequest.RejectionReason,
            docRequest.CreatedAt, docRequest.UpdatedAt);
    }

    public async Task<(byte[] FileContents, string FileName, string ContentType)> DownloadDocumentAsync(Guid id)
    {
        var docRequest = await _context.DocumentRequests
            .Include(r => r.GeneratedDocuments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (docRequest == null)
            throw new KeyNotFoundException("Document request not found");

        var latestDoc = docRequest.GeneratedDocuments
            .OrderByDescending(d => d.Version)
            .FirstOrDefault();

        if (latestDoc == null)
            throw new KeyNotFoundException("No generated document found");

        // TODO: Implement actual file retrieval from storage
        // For now, return a placeholder PDF
        var content = System.Text.Encoding.UTF8.GetBytes(docRequest.ContentFinal ?? docRequest.ContentDraft ?? "");
        return (content, latestDoc.FileName, "application/pdf");
    }
}
