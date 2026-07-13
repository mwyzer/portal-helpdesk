using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Departments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize(Roles = "Super Admin")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IList<DepartmentResponse>>> GetDepartments()
    {
        var result = await _departmentService.GetDepartmentsAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentResponse>> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var result = await _departmentService.CreateDepartmentAsync(request);
        return CreatedAtAction(nameof(GetDepartments), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DepartmentResponse>> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        var result = await _departmentService.UpdateDepartmentAsync(id, request);
        return Ok(result);
    }
}

[ApiController]
[Route("api/positions")]
[Authorize(Roles = "Super Admin")]
public class PositionsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public PositionsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IList<PositionResponse>>> GetPositions([FromQuery] Guid? departmentId)
    {
        var result = await _departmentService.GetPositionsAsync(departmentId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PositionResponse>> CreatePosition([FromBody] CreatePositionRequest request)
    {
        var result = await _departmentService.CreatePositionAsync(request);
        return CreatedAtAction(nameof(GetPositions), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PositionResponse>> UpdatePosition(Guid id, [FromBody] UpdatePositionRequest request)
    {
        var result = await _departmentService.UpdatePositionAsync(id, request);
        return Ok(result);
    }
}
