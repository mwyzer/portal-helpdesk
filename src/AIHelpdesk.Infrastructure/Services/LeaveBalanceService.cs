using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.LeaveBalances;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class LeaveBalanceService : ILeaveBalanceService
{
    private readonly ApplicationDbContext _context;

    public LeaveBalanceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<LeaveBalanceResponse>> GetMyBalancesAsync(Guid userId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null)
            throw new KeyNotFoundException("Employee profile not found");

        return await GetBalancesQuery(employee.Id).ToListAsync();
    }

    public async Task<IList<LeaveBalanceResponse>> GetEmployeeBalancesAsync(Guid employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new KeyNotFoundException("Employee not found");

        return await GetBalancesQuery(employeeId).ToListAsync();
    }

    public async Task AdjustBalanceAsync(AdjustLeaveBalanceRequest request)
    {
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb =>
                lb.EmployeeId == request.EmployeeId &&
                lb.LeaveTypeId == request.LeaveTypeId &&
                lb.Year == request.Year);

        if (balance == null)
        {
            balance = new LeaveBalance
            {
                EmployeeId = request.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,
                Year = request.Year,
                TotalDays = request.AdjustmentDays,
                UsedDays = 0,
                PendingDays = 0
            };
            _context.LeaveBalances.Add(balance);
        }
        else
        {
            balance.TotalDays += request.AdjustmentDays;
            balance.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task InitializeYearlyBalancesAsync(Guid employeeId, int year)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new KeyNotFoundException("Employee not found");

        var leaveTypes = await _context.LeaveTypes.Where(lt => lt.IsActive && !lt.IsDeleted).ToListAsync();

        foreach (var leaveType in leaveTypes)
        {
            var exists = await _context.LeaveBalances.AnyAsync(lb =>
                lb.EmployeeId == employeeId &&
                lb.LeaveTypeId == leaveType.Id &&
                lb.Year == year);

            if (!exists)
            {
                _context.LeaveBalances.Add(new LeaveBalance
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = leaveType.Id,
                    Year = year,
                    TotalDays = leaveType.DaysPerYear,
                    UsedDays = 0,
                    PendingDays = 0
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    private IQueryable<LeaveBalanceResponse> GetBalancesQuery(Guid employeeId)
    {
        var currentYear = DateTime.UtcNow.Year;

        return _context.LeaveBalances
            .Include(lb => lb.LeaveType)
            .Include(lb => lb.Employee)
            .Where(lb => lb.EmployeeId == employeeId && lb.Year == currentYear)
            .Select(lb => new LeaveBalanceResponse(
                lb.Id, lb.EmployeeId, lb.Employee.FullName,
                lb.LeaveTypeId, lb.LeaveType.Name,
                lb.Year, lb.TotalDays, lb.UsedDays, lb.PendingDays,
                lb.TotalDays - lb.UsedDays - lb.PendingDays));
    }
}
