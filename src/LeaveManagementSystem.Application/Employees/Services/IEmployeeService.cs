using LeaveManagementSystem.Application.Employees.Dtos;

namespace LeaveManagementSystem.Application.Employees.Services;

public interface IEmployeeService
{
    Task<EmployeeDto?> UpdateAsync(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
