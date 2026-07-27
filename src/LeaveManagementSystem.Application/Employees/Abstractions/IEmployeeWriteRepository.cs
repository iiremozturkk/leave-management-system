using LeaveManagementSystem.Domain.Entities;

namespace LeaveManagementSystem.Application.Employees.Abstractions;

public interface IEmployeeWriteRepository
{
    Task<bool> DepartmentExistsAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveEmployeeExistsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        Guid? excludedEmployeeId,
        CancellationToken cancellationToken = default);

    void Add(Employee employee);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
