using LeaveManagementSystem.Application.Employees.Dtos;

namespace LeaveManagementSystem.Application.Employees.Abstractions;

public interface IEmployeeReadRepository
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<EmployeeDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
