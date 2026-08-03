using System.Security.Claims;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/job-vacancies")]
[Authorize]
public class JobVacanciesController : ControllerBase
{
    private readonly IJobVacancyService _service;
    private readonly IRecruitmentAIService _aiService;

    public JobVacanciesController(IJobVacancyService service, IRecruitmentAIService aiService)
    {
        _service = service;
        _aiService = aiService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<JobVacancyResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] Guid? departmentId = null)
    {
        var result = await _service.GetAllAsync(page, pageSize, status, departmentId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobVacancyResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "HRD,Manager,Super Admin")]
    public async Task<ActionResult<JobVacancyResponse>> Create([FromBody] CreateJobVacancyRequest request)
    {
        var result = await _service.CreateAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HRD,Manager,Super Admin")]
    public async Task<ActionResult<JobVacancyResponse>> Update(Guid id, [FromBody] UpdateJobVacancyRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "HRD,Manager,Super Admin")]
    public async Task<ActionResult<JobVacancyResponse>> Publish(Guid id)
    {
        var result = await _service.PublishAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "HRD,Manager,Super Admin")]
    public async Task<ActionResult<JobVacancyResponse>> Close(Guid id)
    {
        var result = await _service.CloseAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/ai-match")]
    [Authorize(Roles = "HRD,Manager,Super Admin")]
    public async Task<ActionResult<IList<CandidateMatchResponse>>> MatchCandidates(Guid id)
    {
        var result = await _aiService.MatchCandidatesAsync(id);
        return Ok(result);
    }
}
