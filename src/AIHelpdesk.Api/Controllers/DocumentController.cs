using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/document-templates")]
[Authorize]
public class DocumentTemplatesController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentTemplatesController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<ActionResult<IList<DocumentTemplateResponse>>> GetTemplates([FromQuery] string? category = null)
    {
        var result = await _documentService.GetTemplatesAsync(category);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentTemplateResponse>> GetTemplate(Guid id)
    {
        var result = await _documentService.GetTemplateByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Secretary,Super Admin")]
    public async Task<ActionResult<DocumentTemplateResponse>> CreateTemplate([FromBody] CreateDocumentTemplateRequest request)
    {
        var result = await _documentService.CreateTemplateAsync(request);
        return CreatedAtAction(nameof(GetTemplate), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Secretary,Super Admin")]
    public async Task<ActionResult<DocumentTemplateResponse>> UpdateTemplate(Guid id, [FromBody] UpdateDocumentTemplateRequest request)
    {
        var result = await _documentService.UpdateTemplateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult> DeleteTemplate(Guid id)
    {
        await _documentService.DeleteTemplateAsync(id);
        return NoContent();
    }
}

[ApiController]
[Route("api/document-requests")]
[Authorize]
public class DocumentRequestsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentRequestsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentRequestResponse>>> GetDocumentRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var result = await _documentService.GetDocumentRequestsAsync(GetUserId(), page, pageSize, status);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentRequestDetailResponse>> GetDocumentRequest(Guid id)
    {
        var result = await _documentService.GetDocumentRequestByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DocumentRequestResponse>> CreateDocumentRequest([FromBody] CreateDocumentRequestRequest request)
    {
        var result = await _documentService.CreateDocumentRequestAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetDocumentRequest), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DocumentRequestResponse>> UpdateDocumentRequest(Guid id, [FromBody] UpdateDocumentRequestRequest request)
    {
        var result = await _documentService.UpdateDocumentRequestAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id}/generate-draft")]
    [Authorize(Roles = "Secretary,Super Admin")]
    public async Task<ActionResult<DocumentRequestResponse>> GenerateDraft(Guid id)
    {
        var result = await _documentService.GenerateDraftAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/submit-for-review")]
    [Authorize(Roles = "Secretary,Super Admin")]
    public async Task<ActionResult<DocumentRequestResponse>> SubmitForReview(Guid id)
    {
        var result = await _documentService.SubmitForReviewAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Manager,HRD,Super Admin")]
    public async Task<ActionResult<DocumentRequestResponse>> ApproveDocument(Guid id)
    {
        var result = await _documentService.ApproveDocumentAsync(id, GetUserId());
        return Ok(result);
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Manager,HRD,Super Admin")]
    public async Task<ActionResult<DocumentRequestResponse>> RejectDocument(Guid id, [FromBody] string reason)
    {
        var result = await _documentService.RejectDocumentAsync(id, reason);
        return Ok(result);
    }

    [HttpPost("{id}/generate-final")]
    [Authorize(Roles = "Secretary,Super Admin")]
    public async Task<ActionResult<DocumentRequestResponse>> GenerateFinal(Guid id)
    {
        var result = await _documentService.GenerateFinalAsync(id);
        return Ok(result);
    }

    [HttpGet("{id}/download")]
    public async Task<ActionResult> DownloadDocument(Guid id)
    {
        var (fileContents, fileName, contentType) = await _documentService.DownloadDocumentAsync(id);
        return File(fileContents, contentType, fileName);
    }
}
