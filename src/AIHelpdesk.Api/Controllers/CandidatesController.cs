using System.Security.Claims;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/candidates")]
[Authorize(Roles = "HRD,Manager,Super Admin")]
public class CandidatesController : ControllerBase
{
    private readonly ICandidateService _service;
    private readonly IRecruitmentAIService _aiService;

    public CandidatesController(ICandidateService service, IRecruitmentAIService aiService)
    {
        _service = service;
        _aiService = aiService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<CandidateResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? stage = null, [FromQuery] Guid? jobVacancyId = null)
    {
        var result = await _service.GetAllAsync(page, pageSize, stage, jobVacancyId);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<RecruitmentStatsResponse>> GetStats()
    {
        var result = await _service.GetStatsAsync();
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] Guid? jobVacancyId, [FromQuery] string? stage)
    {
        var data = await _service.ExportToExcelAsync(jobVacancyId, stage);
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "candidates-export.xlsx");
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CandidateDetailResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CandidateResponse>> Create([FromBody] CreateCandidateRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CandidateResponse>> Update(Guid id, [FromBody] UpdateCandidateRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cv")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<CandidateDocumentResponse>> UploadCv(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        await using var stream = file.OpenReadStream();
        var result = await _service.UploadCvAsync(id, GetUserId(), file.FileName, file.ContentType, stream);
        return Ok(result);
    }

    [HttpGet("{id:guid}/cv/{documentId:guid}")]
    public async Task<IActionResult> DownloadCv(Guid id, Guid documentId)
    {
        var (stream, contentType, fileName) = await _service.DownloadCvAsync(id, documentId);
        return File(stream, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType, fileName);
    }

    [HttpDelete("{id:guid}/cv/{documentId:guid}")]
    public async Task<IActionResult> DeleteCv(Guid id, Guid documentId)
    {
        await _service.DeleteCvAsync(id, documentId);
        return NoContent();
    }

    [HttpPost("{id:guid}/advance-stage")]
    public async Task<ActionResult<CandidateResponse>> AdvanceStage(Guid id, [FromBody] AdvanceCandidateStageRequest request)
    {
        var result = await _service.AdvanceStageAsync(id, GetUserId(), request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<CandidateResponse>> Reject(Guid id, [FromBody] RejectCandidateRequest request)
    {
        var result = await _service.RejectAsync(id, GetUserId(), request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/ai-summarize")]
    public async Task<ActionResult<CvSummarizeResponse>> SummarizeCv(Guid id)
    {
        var result = await _aiService.SummarizeCvAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/portal-invite")]
    public async Task<ActionResult<CandidatePortalInviteResponse>> RegenerateInvite(Guid id)
    {
        var result = await _service.RegenerateInviteAsync(id);
        return Ok(result);
    }
}
