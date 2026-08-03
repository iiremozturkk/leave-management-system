using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.Authentication.Persistence;

public sealed class UserAccountReadRepository(
    AppDbContext dbContext)
    : IUserAccountReadRepository
{
    public Task<UserAccountAuthenticationData?> GetByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .AsNoTracking()
            .Where(
                userAccount =>
                    userAccount.Employee.Email ==
                    normalizedEmail)
            .Select(
                userAccount =>
                    new UserAccountAuthenticationData(
                        userAccount.Id,
                        userAccount.EmployeeId,
                        userAccount.Employee.Email,
                        userAccount.Employee.Role,
                        userAccount.IsActive,
                        userAccount.Employee.IsActive,
                        userAccount.PasswordHash))
            .FirstOrDefaultAsync(
                cancellationToken);
    }
}
