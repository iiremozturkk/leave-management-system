namespace LeaveManagementSystem.Application.Employees.Abstractions;

public interface IEmployeeAdministrationTransaction
    : IAsyncDisposable
{
    Task CommitAsync(
        CancellationToken cancellationToken = default);
}
