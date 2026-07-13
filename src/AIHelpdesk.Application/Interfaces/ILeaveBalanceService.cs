using AIHelpdesk.Contracts.LeaveBalances;

namespace AIHelpdesk.Application.Interfaces;

public interface ILeaveBalanceService
{
    Task<IList<LeaveBalanceResponse>> GetMyBalancesAsync(Guid userId);
    Task<IList<LeaveBalanceResponse>> GetEmployeeBalancesAsync(Guid employeeId);
    Task AdjustBalanceAsync(AdjustLeaveBalanceRequest request);
    Task InitializeYearlyBalancesAsync(Guid employeeId, int year);
}
