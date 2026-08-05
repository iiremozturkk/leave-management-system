using LeaveManagementSystem.Domain.Entities;

namespace LeaveManagementSystem.Application.Employees.Abstractions;

public interface IEmployeeWriteRepository
{
    Task<Employee?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> DepartmentExistsAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveManagerExistsAsync(
        Guid managerId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetManagerIdAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveDirectReportsAsync(
        Guid managerId,
        CancellationToken cancellationToken = default);

    Task<bool> IsSoleActiveHrAdministratorAsync(
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
