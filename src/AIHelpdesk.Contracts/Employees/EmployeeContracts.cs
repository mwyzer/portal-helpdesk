namespace AIHelpdesk.Contracts.Employees;

public record CreateEmployeeRequest(
    string EmployeeNo,
    string FullName,
    string Email,
    string? Phone,
    DateOnly JoinDate,
    Guid? DepartmentId,
    Guid? PositionId,
    Guid? ManagerId,
    string? WorkLocation
);

public record UpdateEmployeeRequest(
    string FullName,
    string? Phone,
    Guid? DepartmentId,
    Guid? PositionId,
    Guid? ManagerId,
    string? WorkLocation
);

public record EmployeeResponse(
    Guid Id,
    string EmployeeNo,
    string FullName,
    string Email,
    string? Phone,
    DateOnly JoinDate,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? PositionId,
    string? PositionName,
    Guid? ManagerId,
    string? ManagerName,
    string EmploymentStatus,
    string? WorkLocation,
    Guid? UserId,
    DateTime CreatedAt
);

public record EmployeeListResponse(
    IList<EmployeeResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record EmployeeImportResult(
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    IList<EmployeeImportError> Errors
);

public record EmployeeImportError(
    int Row,
    string Message
);
