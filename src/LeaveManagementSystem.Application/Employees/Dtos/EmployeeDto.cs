using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Application.Employees.Dtos;

public sealed record EmployeeDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    EmployeeRole Role,
    bool IsActive,
    Guid DepartmentId,
    string DepartmentName,
    Guid? ManagerId,
    string? ManagerFullName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
