using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Employees;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

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
        var errors = new List<EmployeeImportError>();
        var successCount = 0;
        var totalRows = 0;

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // skip header

        if (rows == null)
            return new EmployeeImportResult(0, 0, 0, errors);

        foreach (var row in rows)
        {
            totalRows++;
            try
            {
                var employeeNo = row.Cell(1).GetString().Trim();
                var fullName = row.Cell(2).GetString().Trim();
                var email = row.Cell(3).GetString().Trim();
                var phone = row.Cell(4).GetString()?.Trim();
                var joinDateStr = row.Cell(5).GetString()?.Trim();
                var departmentName = row.Cell(6).GetString()?.Trim();
                var positionName = row.Cell(7).GetString()?.Trim();
                var workLocation = row.Cell(8).GetString()?.Trim();

                // Validate required fields
                if (string.IsNullOrWhiteSpace(employeeNo))
                { errors.Add(new EmployeeImportError(totalRows + 1, "Employee number is required")); continue; }
                if (string.IsNullOrWhiteSpace(fullName))
                { errors.Add(new EmployeeImportError(totalRows + 1, "Full name is required")); continue; }
                if (string.IsNullOrWhiteSpace(email))
                { errors.Add(new EmployeeImportError(totalRows + 1, "Email is required")); continue; }

                // Check duplicate employee number
                var exists = await _context.Employees.AnyAsync(e => e.EmployeeNo == employeeNo);
                if (exists)
                { errors.Add(new EmployeeImportError(totalRows + 1, $"Employee number '{employeeNo}' already exists")); continue; }

                // Parse join date
                if (!DateOnly.TryParse(joinDateStr, out var joinDate))
                { errors.Add(new EmployeeImportError(totalRows + 1, $"Invalid join date: '{joinDateStr}'")); continue; }

                // Resolve department
                Guid? departmentId = null;
                if (!string.IsNullOrWhiteSpace(departmentName))
                {
                    var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Name == departmentName);
                    if (dept == null)
                    { errors.Add(new EmployeeImportError(totalRows + 1, $"Department not found: '{departmentName}'")); continue; }
                    departmentId = dept.Id;
                }

                // Resolve position
                Guid? positionId = null;
                if (!string.IsNullOrWhiteSpace(positionName))
                {
                    var pos = await _context.Positions.FirstOrDefaultAsync(p => p.Name == positionName);
                    if (pos == null)
                    { errors.Add(new EmployeeImportError(totalRows + 1, $"Position not found: '{positionName}'")); continue; }
                    positionId = pos.Id;
                }

                var employee = new Employee
                {
                    EmployeeNo = employeeNo,
                    FullName = fullName,
                    Email = email,
                    Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                    JoinDate = joinDate,
                    DepartmentId = departmentId,
                    PositionId = positionId,
                    WorkLocation = string.IsNullOrWhiteSpace(workLocation) ? null : workLocation,
                    EmploymentStatus = EmploymentStatus.Active,
                };

                _context.Employees.Add(employee);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new EmployeeImportError(totalRows + 1, ex.Message));
            }
        }

        if (successCount > 0)
            await _context.SaveChangesAsync();

        return new EmployeeImportResult(totalRows, successCount, errors.Count, errors);
    }

    public async Task<byte[]> ExportToExcelAsync(string? search, Guid? departmentId, string? status)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
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

        var employees = await query
            .OrderBy(e => e.EmployeeNo)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Employees");

        // Header
        var headers = new[] { "Employee No", "Full Name", "Email", "Phone", "Join Date", "Department", "Position", "Status", "Work Location" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data rows
        for (int i = 0; i < employees.Count; i++)
        {
            var e = employees[i];
            var row = i + 2;
            worksheet.Cell(row, 1).Value = e.EmployeeNo;
            worksheet.Cell(row, 2).Value = e.FullName;
            worksheet.Cell(row, 3).Value = e.Email;
            worksheet.Cell(row, 4).Value = e.Phone ?? "";
            worksheet.Cell(row, 5).Value = e.JoinDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 6).Value = e.Department?.Name ?? "";
            worksheet.Cell(row, 7).Value = e.Position?.Name ?? "";
            worksheet.Cell(row, 8).Value = e.EmploymentStatus.ToString();
            worksheet.Cell(row, 9).Value = e.WorkLocation ?? "";
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static EmployeeResponse MapToResponse(Employee e) => new(
        e.Id, e.EmployeeNo, e.FullName, e.Email, e.Phone,
        e.JoinDate, e.DepartmentId, e.Department?.Name,
        e.PositionId, e.Position?.Name,
        e.ManagerId, e.Manager?.FullName,
        e.EmploymentStatus.ToString(), e.WorkLocation,
        e.UserId, e.CreatedAt);
}
