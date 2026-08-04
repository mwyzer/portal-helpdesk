using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Recruitment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/interviews")]
[Authorize(Roles = "HRD,Manager,Super Admin")]
public class InterviewsController : ControllerBase
{
    private readonly IInterviewService _service;
    private readonly IRecruitmentAIService _aiService;

    public InterviewsController(IInterviewService service, IRecruitmentAIService aiService)
    {
        _service = service;
        _aiService = aiService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InterviewResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null, [FromQuery] Guid? candidateId = null)
    {
        var result = await _service.GetAllAsync(page, pageSize, fromDate, toDate, candidateId);
        return Ok(result);
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<IList<InterviewResponse>>> GetUpcoming([FromQuery] Guid? interviewerId = null)
    {
        var result = await _service.GetUpcomingAsync(interviewerId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InterviewResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<InterviewResponse>> Create([FromBody] CreateInterviewRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InterviewResponse>> Update(Guid id, [FromBody] UpdateInterviewRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<InterviewResponse>> Complete(Guid id, [FromBody] CompleteInterviewRequest request)
    {
        var result = await _service.CompleteAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<InterviewResponse>> Cancel(Guid id)
    {
        var result = await _service.CancelAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/ai-questions")]
    public async Task<ActionResult<InterviewQuestionsResponse>> GenerateQuestions(Guid id)
    {
        var result = await _aiService.GenerateInterviewQuestionsAsync(id);
        return Ok(result);
    }

    // ── Interview slots: staff opens them, candidates book them via the self-service portal ──

    [HttpGet("slots")]
    public async Task<ActionResult<IList<InterviewSlotResponse>>> GetSlots(
        [FromQuery] Guid? jobVacancyId = null, [FromQuery] Guid? interviewerId = null, [FromQuery] string? status = null)
    {
        var result = await _service.GetSlotsAsync(jobVacancyId, interviewerId, status);
        return Ok(result);
    }

    [HttpPost("slots")]
    public async Task<ActionResult<InterviewSlotResponse>> CreateSlot([FromBody] CreateInterviewSlotRequest request)
    {
        var result = await _service.CreateSlotAsync(request);
        return Ok(result);
    }

    [HttpPost("slots/{slotId:guid}/cancel")]
    public async Task<IActionResult> CancelSlot(Guid slotId)
    {
        await _service.CancelSlotAsync(slotId);
        return NoContent();
    }
}
