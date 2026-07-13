using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.LeaveTypes;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class LeaveTypeService : ILeaveTypeService
{
    private readonly ApplicationDbContext _context;

    public LeaveTypeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<LeaveTypeResponse>> GetAllAsync()
    {
        return await _context.LeaveTypes
            .OrderBy(lt => lt.Name)
            .Select(lt => new LeaveTypeResponse(
                lt.Id, lt.Name, lt.Code, lt.DaysPerYear,
                lt.IsPaid, lt.IsActive, lt.MinServiceMonths,
                lt.RequiresAttachment, lt.SkipManagerApproval,
                lt.CreatedAt))
            .ToListAsync();
    }

    public async Task<LeaveTypeResponse> GetByIdAsync(Guid id)
    {
        var lt = await _context.LeaveTypes.FindAsync(id);
        if (lt == null)
            throw new KeyNotFoundException("Leave type not found");

        return new LeaveTypeResponse(
            lt.Id, lt.Name, lt.Code, lt.DaysPerYear,
            lt.IsPaid, lt.IsActive, lt.MinServiceMonths,
            lt.RequiresAttachment, lt.SkipManagerApproval,
            lt.CreatedAt);
    }

    public async Task<LeaveTypeResponse> CreateAsync(CreateLeaveTypeRequest request)
    {
        var leaveType = new LeaveType
        {
            Name = request.Name,
            Code = request.Code,
            DaysPerYear = request.DaysPerYear,
            IsPaid = request.IsPaid,
            MinServiceMonths = request.MinServiceMonths,
            RequiresAttachment = request.RequiresAttachment,
            SkipManagerApproval = request.SkipManagerApproval,
            IsActive = true
        };

        _context.LeaveTypes.Add(leaveType);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(leaveType.Id);
    }

    public async Task<LeaveTypeResponse> UpdateAsync(Guid id, UpdateLeaveTypeRequest request)
    {
        var lt = await _context.LeaveTypes.FindAsync(id);
        if (lt == null)
            throw new KeyNotFoundException("Leave type not found");

        lt.Name = request.Name;
        lt.DaysPerYear = request.DaysPerYear;
        lt.IsPaid = request.IsPaid;
        lt.IsActive = request.IsActive;
        lt.MinServiceMonths = request.MinServiceMonths;
        lt.RequiresAttachment = request.RequiresAttachment;
        lt.SkipManagerApproval = request.SkipManagerApproval;
        lt.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var lt = await _context.LeaveTypes.FindAsync(id);
        if (lt == null)
            throw new KeyNotFoundException("Leave type not found");

        lt.IsDeleted = true;
        lt.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
