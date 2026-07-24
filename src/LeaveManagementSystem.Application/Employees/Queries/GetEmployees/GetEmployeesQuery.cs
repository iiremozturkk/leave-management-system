using LeaveManagementSystem.Application.Employees.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.Employees.Queries.GetEmployees;

public sealed record GetEmployeesQuery
    : IRequest<IReadOnlyList<EmployeeDto>>;
