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
    // Secretary can't browse the Employees module itself (no menu access), but needs to read
    // this list to populate assignee/participant pickers on Action Items and Meetings, both of
    // which Secretary does have access to.
    [Authorize(Roles = "Super Admin,HRD,Manager,Secretary")]
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

    [HttpGet("import-template")]
    [Authorize(Roles = "Super Admin,HRD")]
    public async Task<IActionResult> DownloadImportTemplate()
    {
        var data = await _employeeService.GenerateImportTemplateAsync();
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "employee-import-template.xlsx");
    }

    [HttpPost("import")]
    [Authorize(Roles = "Super Admin,HRD")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<ActionResult<EmployeeImportResult>> ImportEmployees(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx files are supported");

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
