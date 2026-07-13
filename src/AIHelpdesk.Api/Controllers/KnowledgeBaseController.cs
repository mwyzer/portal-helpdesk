using System.Security.Claims;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/knowledge-documents")]
[Authorize]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeBaseService _kbService;

    public KnowledgeBaseController(IKnowledgeBaseService kbService)
    {
        _kbService = kbService;
    }

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
    [Authorize(Roles = "Secretary,HR Admin,Super Admin")]
    public async Task<ActionResult<KnowledgeDocumentResponse>> Upload(
        [FromForm] string title, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required");

        using var stream = file.OpenReadStream();
        var result = await _kbService.UploadDocumentAsync(
            Guid.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!),
            title, file.FileName, stream, file.ContentType);
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
    [Authorize(Roles = "Secretary,Super Admin")]
    public async Task<ActionResult<KnowledgeDocumentResponse>> ReIndex(Guid id)
    {
        var result = await _kbService.IndexDocumentAsync(id);
        return Ok(result);
    }

    [HttpPost("search")]
    public async Task<ActionResult<List<KnowledgeSearchResult>>> Search([FromBody] SearchKnowledgeRequest request)
    {
        var results = await _kbService.SearchAsync(request.Query, request.TopK);
        return Ok(results);
    }
}
