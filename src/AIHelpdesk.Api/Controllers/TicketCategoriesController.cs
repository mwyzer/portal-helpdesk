using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/ticket-categories")]
[Authorize]
public class TicketCategoriesController : ControllerBase
{
    private readonly ITicketCategoryService _service;

    public TicketCategoriesController(ITicketCategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IList<TicketCategoryResponse>>> GetAll([FromQuery] Guid? departmentId)
    {
        var result = await _service.GetAllAsync(departmentId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketCategoryResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Super Admin,Manager")]
    public async Task<ActionResult<TicketCategoryResponse>> Create([FromBody] CreateTicketCategoryRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Super Admin,Manager")]
    public async Task<ActionResult<TicketCategoryResponse>> Update(Guid id, [FromBody] UpdateTicketCategoryRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
