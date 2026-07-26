using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.Employees.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryHandler(
    IEmployeeReadRepository employeeReadRepository)
    : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto?>
{
    public Task<EmployeeDto?> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        return employeeReadRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
