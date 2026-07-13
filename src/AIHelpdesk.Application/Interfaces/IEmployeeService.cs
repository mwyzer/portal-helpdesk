using AIHelpdesk.Contracts.Employees;

namespace AIHelpdesk.Application.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeListResponse> GetEmployeesAsync(int page, int pageSize, string? search, Guid? departmentId, string? status);
    Task<EmployeeResponse> GetEmployeeAsync(Guid id);
    Task<EmployeeResponse> GetMyProfileAsync(Guid userId);
    Task<EmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request);
    Task<EmployeeResponse> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request);
    Task DeleteEmployeeAsync(Guid id);
    Task<EmployeeImportResult> ImportFromExcelAsync(Stream fileStream);
    Task<byte[]> ExportToExcelAsync(string? search, Guid? departmentId, string? status);
}
