using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Tickets;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class AgentAssignmentService : IAgentAssignmentService
{
    private readonly ApplicationDbContext _context;

    public AgentAssignmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<AgentAssignmentResponse>> GetByDepartmentAsync(Guid departmentId)
    {
        return await _context.AgentAssignments
            .Where(a => a.DepartmentId == departmentId)
            .Include(a => a.User)
            .Include(a => a.Department)
            .OrderBy(a => a.User.FullName)
            .Select(a => new AgentAssignmentResponse(
                a.Id, a.UserId, a.User.FullName, a.DepartmentId, a.Department.Name,
                a.IsActive, a.MaxTickets, a.CurrentLoad, a.CreatedAt))
            .ToListAsync();
    }

    public async Task<IList<AgentAssignmentResponse>> GetAllAsync()
    {
        return await _context.AgentAssignments
            .Include(a => a.User)
            .Include(a => a.Department)
            .OrderBy(a => a.Department.Name).ThenBy(a => a.User.FullName)
            .Select(a => new AgentAssignmentResponse(
                a.Id, a.UserId, a.User.FullName, a.DepartmentId, a.Department.Name,
                a.IsActive, a.MaxTickets, a.CurrentLoad, a.CreatedAt))
            .ToListAsync();
    }

    public async Task<AgentAssignmentResponse> CreateAsync(CreateAgentAssignmentRequest request)
    {
        var assignment = new AgentAssignment
        {
            UserId = request.UserId,
            DepartmentId = request.DepartmentId,
            MaxTickets = request.MaxTickets,
            IsActive = true,
            CurrentLoad = 0
        };

        _context.AgentAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        await _context.Entry(assignment).Reference(a => a.User).LoadAsync();
        await _context.Entry(assignment).Reference(a => a.Department).LoadAsync();

        return new AgentAssignmentResponse(
            assignment.Id, assignment.UserId, assignment.User.FullName,
            assignment.DepartmentId, assignment.Department.Name,
            assignment.IsActive, assignment.MaxTickets, assignment.CurrentLoad, assignment.CreatedAt);
    }

    public async Task<AgentAssignmentResponse> UpdateAsync(Guid id, UpdateAgentAssignmentRequest request)
    {
        var assignment = await _context.AgentAssignments
            .Include(a => a.User)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("Agent assignment not found");

        assignment.MaxTickets = request.MaxTickets;
        assignment.IsActive = request.IsActive;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new AgentAssignmentResponse(
            assignment.Id, assignment.UserId, assignment.User.FullName,
            assignment.DepartmentId, assignment.Department.Name,
            assignment.IsActive, assignment.MaxTickets, assignment.CurrentLoad, assignment.CreatedAt);
    }

    public async Task DeleteAsync(Guid id)
    {
        var assignment = await _context.AgentAssignments.FindAsync(id)
            ?? throw new KeyNotFoundException("Agent assignment not found");

        assignment.IsDeleted = true;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<Guid?> GetNextAvailableAgentAsync(Guid departmentId)
    {
        return await _context.AgentAssignments
            .Where(a => a.DepartmentId == departmentId && a.IsActive && a.CurrentLoad < a.MaxTickets)
            .OrderBy(a => a.CurrentLoad)
            .Select(a => (Guid?)a.UserId)
            .FirstOrDefaultAsync();
    }
}
