namespace AIHelpdesk.Contracts.Departments;

public record CreateDepartmentRequest(string Name, string Code);

public record UpdateDepartmentRequest(string Name, string Code, bool IsActive);

public record DepartmentResponse(Guid Id, string Name, string Code, bool IsActive, int PositionCount);

public record PositionResponse(Guid Id, string Name, Guid DepartmentId, string? Department, bool IsActive);

public record CreatePositionRequest(string Name, Guid DepartmentId);

public record UpdatePositionRequest(string Name, Guid DepartmentId, bool IsActive);
