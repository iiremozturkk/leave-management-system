using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.Authentication.Persistence;

public sealed class CurrentUserAccessReadRepository(
    AppDbContext dbContext)
    : ICurrentUserAccessReadRepository
{
    public Task<CurrentUserAccessData?> GetByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .AsNoTracking()
            .Where(
                userAccount =>
                    userAccount.Id == userAccountId)
            .Select(
                userAccount =>
                    new CurrentUserAccessData(
                        userAccount.Id,
                        userAccount.EmployeeId,
                        userAccount.Employee.Email,
                        userAccount.Employee.Role,
                        userAccount.IsActive,
                        userAccount.Employee.IsActive))
            .SingleOrDefaultAsync(
                cancellationToken);
    }
}
