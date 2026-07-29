namespace LeaveManagementSystem.Application.Employees.Services;

public interface IEmployeeService
{
    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
