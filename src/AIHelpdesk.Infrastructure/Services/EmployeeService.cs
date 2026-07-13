using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Employees;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeListResponse> GetEmployeesAsync(int page, int pageSize, string? search, Guid? departmentId, string? status)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e =>
                e.FullName.Contains(search) ||
                e.Email.Contains(search) ||
                e.EmployeeNo.Contains(search));

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EmploymentStatus>(status, out var empStatus))
            query = query.Where(e => e.EmploymentStatus == empStatus);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(e => e.EmployeeNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeResponse(
                e.Id, e.EmployeeNo, e.FullName, e.Email, e.Phone,
                e.JoinDate, e.DepartmentId, e.Department!.Name,
                e.PositionId, e.Position!.Name,
                e.ManagerId, e.Manager!.FullName,
                e.EmploymentStatus.ToString(), e.WorkLocation,
                e.UserId, e.CreatedAt))
            .ToListAsync();

        return new EmployeeListResponse(items, totalCount, page, pageSize);
    }

    public async Task<EmployeeResponse> GetEmployeeAsync(Guid id)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            throw new KeyNotFoundException("Employee not found");

        return MapToResponse(employee);
    }

    public async Task<EmployeeResponse> GetMyProfileAsync(Guid userId)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee == null)
            throw new KeyNotFoundException("Employee profile not found");

        return MapToResponse(employee);
    }

    public async Task<EmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        var employee = new Employee
        {
            EmployeeNo = request.EmployeeNo,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            JoinDate = request.JoinDate,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            ManagerId = request.ManagerId,
            WorkLocation = request.WorkLocation,
            EmploymentStatus = EmploymentStatus.Active
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return await GetEmployeeAsync(employee.Id);
    }

    public async Task<EmployeeResponse> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
            throw new KeyNotFoundException("Employee not found");

        employee.FullName = request.FullName;
        employee.Phone = request.Phone;
        employee.DepartmentId = request.DepartmentId;
        employee.PositionId = request.PositionId;
        employee.ManagerId = request.ManagerId;
        employee.WorkLocation = request.WorkLocation;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetEmployeeAsync(id);
    }

    public async Task DeleteEmployeeAsync(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
            throw new KeyNotFoundException("Employee not found");

        employee.IsDeleted = true;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<EmployeeImportResult> ImportFromExcelAsync(Stream fileStream)
    {
        // TODO: Implement Excel import using ClosedXML
        await Task.CompletedTask;
        throw new NotImplementedException("Excel import not yet implemented");
    }

    public async Task<byte[]> ExportToExcelAsync(string? search, Guid? departmentId, string? status)
    {
        // TODO: Implement Excel export using ClosedXML
        await Task.CompletedTask;
        throw new NotImplementedException("Excel export not yet implemented");
    }

    private static EmployeeResponse MapToResponse(Employee e) => new(
        e.Id, e.EmployeeNo, e.FullName, e.Email, e.Phone,
        e.JoinDate, e.DepartmentId, e.Department?.Name,
        e.PositionId, e.Position?.Name,
        e.ManagerId, e.Manager?.FullName,
        e.EmploymentStatus.ToString(), e.WorkLocation,
        e.UserId, e.CreatedAt);
}
