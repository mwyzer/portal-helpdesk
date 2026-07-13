using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIHelpdesk.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "Super Admin,HRD,Manager")]
    public async Task<ActionResult<EmployeeListResponse>> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? status = null)
    {
        var result = await _employeeService.GetEmployeesAsync(page, pageSize, search, departmentId, status);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Super Admin,HRD,Manager")]
    public async Task<ActionResult<EmployeeResponse>> GetEmployee(Guid id)
    {
        var result = await _employeeService.GetEmployeeAsync(id);
        return Ok(result);
    }

    [HttpGet("my-profile")]
    public async Task<ActionResult<EmployeeResponse>> GetMyProfile()
    {
        var result = await _employeeService.GetMyProfileAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Super Admin,HRD")]
    public async Task<ActionResult<EmployeeResponse>> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        var result = await _employeeService.CreateEmployeeAsync(request);
        return CreatedAtAction(nameof(GetEmployee), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Super Admin,HRD")]
    public async Task<ActionResult<EmployeeResponse>> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        var result = await _employeeService.UpdateEmployeeAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult> DeleteEmployee(Guid id)
    {
        await _employeeService.DeleteEmployeeAsync(id);
        return NoContent();
    }

    [HttpPost("import")]
    [Authorize(Roles = "Super Admin,HRD")]
    public async Task<ActionResult<EmployeeImportResult>> ImportEmployees(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var result = await _employeeService.ImportFromExcelAsync(stream);
        return Ok(result);
    }

    [HttpGet("export")]
    [Authorize(Roles = "Super Admin,HRD")]
    public async Task<IActionResult> ExportEmployees(
        [FromQuery] string? search = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? status = null)
    {
        var data = await _employeeService.ExportToExcelAsync(search, departmentId, status);
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "employees.xlsx");
    }
}
