using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Departments;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _context;

    public DepartmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<DepartmentResponse>> GetDepartmentsAsync()
    {
        return await _context.Departments
            .Select(d => new DepartmentResponse(
                d.Id, d.Name, d.Code, d.IsActive,
                d.Positions.Count(p => !p.IsDeleted)))
            .ToListAsync();
    }

    public async Task<DepartmentResponse> CreateDepartmentAsync(CreateDepartmentRequest request)
    {
        var dept = new Department
        {
            Name = request.Name,
            Code = request.Code,
            IsActive = true
        };

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        return new DepartmentResponse(dept.Id, dept.Name, dept.Code, dept.IsActive, 0);
    }

    public async Task<DepartmentResponse> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept == null)
            throw new KeyNotFoundException("Department not found");

        dept.Name = request.Name;
        dept.Code = request.Code;
        dept.IsActive = request.IsActive;
        dept.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new DepartmentResponse(dept.Id, dept.Name, dept.Code, dept.IsActive,
            await _context.Positions.CountAsync(p => p.DepartmentId == id && !p.IsDeleted));
    }

    public async Task<IList<PositionResponse>> GetPositionsAsync(Guid? departmentId)
    {
        var query = _context.Positions.Include(p => p.Department).AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(p => p.DepartmentId == departmentId.Value);

        return await query
            .Select(p => new PositionResponse(p.Id, p.Name, p.DepartmentId, p.Department.Name, p.IsActive))
            .ToListAsync();
    }

    public async Task<PositionResponse> CreatePositionAsync(CreatePositionRequest request)
    {
        var dept = await _context.Departments.FindAsync(request.DepartmentId);
        if (dept == null)
            throw new KeyNotFoundException("Department not found");

        var position = new Position
        {
            Name = request.Name,
            DepartmentId = request.DepartmentId,
            IsActive = true
        };

        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        return new PositionResponse(position.Id, position.Name, position.DepartmentId, dept.Name, position.IsActive);
    }

    public async Task<PositionResponse> UpdatePositionAsync(Guid id, UpdatePositionRequest request)
    {
        var position = await _context.Positions.Include(p => p.Department).FirstOrDefaultAsync(p => p.Id == id);
        if (position == null)
            throw new KeyNotFoundException("Position not found");

        position.Name = request.Name;
        position.DepartmentId = request.DepartmentId;
        position.IsActive = request.IsActive;
        position.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new PositionResponse(position.Id, position.Name, position.DepartmentId, position.Department.Name, position.IsActive);
    }
}
