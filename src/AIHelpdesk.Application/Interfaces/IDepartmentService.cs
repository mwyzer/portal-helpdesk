using AIHelpdesk.Contracts.Departments;

namespace AIHelpdesk.Application.Interfaces;

public interface IDepartmentService
{
    Task<IList<DepartmentResponse>> GetDepartmentsAsync();
    Task<DepartmentResponse> CreateDepartmentAsync(CreateDepartmentRequest request);
    Task<DepartmentResponse> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request);
    Task<IList<PositionResponse>> GetPositionsAsync(Guid? departmentId);
    Task<PositionResponse> CreatePositionAsync(CreatePositionRequest request);
    Task<PositionResponse> UpdatePositionAsync(Guid id, UpdatePositionRequest request);
}
