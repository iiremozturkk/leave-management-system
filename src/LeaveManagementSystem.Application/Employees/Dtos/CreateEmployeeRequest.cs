using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Application.Employees.Dtos;

public sealed record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    Guid DepartmentId,
    Guid? ManagerId,
    EmployeeRole Role);
