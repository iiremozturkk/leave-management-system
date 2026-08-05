namespace LeaveManagementSystem.Application.Employees.Abstractions;

public interface IEmployeeAdministrationTransactionManager
{
    Task<IEmployeeAdministrationTransaction> BeginAsync(
        CancellationToken cancellationToken = default);
}
