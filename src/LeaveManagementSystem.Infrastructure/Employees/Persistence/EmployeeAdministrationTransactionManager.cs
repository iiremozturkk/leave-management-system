using System.Data;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LeaveManagementSystem.Infrastructure.Employees.Persistence;

public sealed class EmployeeAdministrationTransactionManager(
    AppDbContext dbContext)
    : IEmployeeAdministrationTransactionManager
{
    private const long ActiveHrAdministratorLockKey =
        741_202_608_05L;

    public async Task<IEmployeeAdministrationTransaction> BeginAsync(
        CancellationToken cancellationToken = default)
    {
        var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        try
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({ActiveHrAdministratorLockKey})",
                cancellationToken);

            return new EmployeeAdministrationTransaction(
                transaction);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private sealed class EmployeeAdministrationTransaction(
        IDbContextTransaction transaction)
        : IEmployeeAdministrationTransaction
    {
        public Task CommitAsync(
            CancellationToken cancellationToken = default)
        {
            return transaction.CommitAsync(
                cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return transaction.DisposeAsync();
        }
    }
}
