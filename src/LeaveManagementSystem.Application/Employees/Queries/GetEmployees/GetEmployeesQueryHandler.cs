using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.Employees.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQueryHandler(
    IEmployeeReadRepository employeeReadRepository)
    : IRequestHandler<GetEmployeesQuery, IReadOnlyList<EmployeeDto>>
{
    public Task<IReadOnlyList<EmployeeDto>> Handle(
        GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        return employeeReadRepository.GetAllAsync(cancellationToken);
    }
}
