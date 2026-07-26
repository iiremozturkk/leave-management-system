using LeaveManagementSystem.Application.Employees.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.Employees.Queries.GetEmployeeById;

public sealed record GetEmployeeByIdQuery(
    Guid Id)
    : IRequest<EmployeeDto?>;
