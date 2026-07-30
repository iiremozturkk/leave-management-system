using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.IntegrationTests.Contracts;

internal sealed record EmployeeResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    Guid DepartmentId,
    Guid? ManagerId,
    EmployeeRole Role,
    bool IsActive);
