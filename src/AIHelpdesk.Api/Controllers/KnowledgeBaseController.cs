using System.Security.Claims;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Knowledge;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/knowledge-documents")]
[Authorize]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeBaseService _kbService;
    private readonly ApplicationDbContext _context;

    public KnowledgeBaseController(IKnowledgeBaseService kbService, ApplicationDbContext context)
    {
        _kbService = kbService;
        _context = context;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<KnowledgeDocumentResponse>>> GetDocuments(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        var result = await _kbService.GetDocumentsAsync(page, pageSize, status);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<KnowledgeDocumentDetailResponse>> GetDocument(Guid id)
    {
        var result = await _kbService.GetDocumentAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Secretary,HRD,Super Admin")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB
    public async Task<ActionResult<KnowledgeDocumentResponse>> Upload(
        [FromForm] string title, IFormFile file, [FromForm] Guid? departmentId = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required");

        using var stream = file.OpenReadStream();
        var result = await _kbService.UploadDocumentAsync(
            GetUserId(), title, file.FileName, stream, file.ContentType, departmentId);
        return CreatedAtAction(nameof(GetDocument), new { id = result.Id }, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult> DeleteDocument(Guid id)
    {
        await _kbService.DeleteDocumentAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/index")]
    [Authorize(Roles = "Secretary,Super Admin,HRD")]
    public async Task<ActionResult<KnowledgeDocumentResponse>> ReIndex(Guid id)
    {
        var result = await _kbService.IndexDocumentAsync(id);
        return Ok(result);
    }

    [HttpPost("search")]
    public async Task<ActionResult<List<KnowledgeSearchResult>>> Search([FromBody] SearchKnowledgeRequest request)
    {
        // Same department scoping as the chat RAG path (ChatService.SendMessageAsync) --
        // without this, any authenticated user could pull chunks from another department's
        // documents straight from this endpoint, bypassing the chat flow's guardrail entirely.
        var requesterDepartmentId = await _context.Users
            .Where(u => u.Id == GetUserId())
            .Select(u => u.DepartmentId)
            .FirstOrDefaultAsync();
        var results = await _kbService.SearchAsync(request.Query, request.TopK, requesterDepartmentId);
        return Ok(results);
    }
}
