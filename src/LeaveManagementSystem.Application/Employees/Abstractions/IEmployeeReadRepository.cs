using LeaveManagementSystem.Application.Employees.Dtos;

namespace LeaveManagementSystem.Application.Employees.Abstractions;

public interface IEmployeeReadRepository
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
}