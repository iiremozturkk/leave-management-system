using LeaveManagementSystem.Application.Employees.Dtos;
using LeaveManagementSystem.Domain.Enums;
using MediatR;

namespace LeaveManagementSystem.Application.Employees.Commands.UpdateEmployee;

public sealed record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    Guid DepartmentId,
    Guid? ManagerId,
    EmployeeRole Role,
    bool IsActive)
    : IRequest<EmployeeDto?>;
