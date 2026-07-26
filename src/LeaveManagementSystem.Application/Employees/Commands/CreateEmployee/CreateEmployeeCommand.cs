using LeaveManagementSystem.Application.Employees.Dtos;
using LeaveManagementSystem.Domain.Enums;
using MediatR;

namespace LeaveManagementSystem.Application.Employees.Commands.CreateEmployee;

public sealed record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    Guid DepartmentId,
    Guid? ManagerId,
    EmployeeRole Role)
    : IRequest<EmployeeDto>;
