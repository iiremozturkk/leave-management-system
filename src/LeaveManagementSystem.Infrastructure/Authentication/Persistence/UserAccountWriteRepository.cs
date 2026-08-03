using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.Authentication.Persistence;

public sealed class UserAccountWriteRepository(
    AppDbContext dbContext)
    : IUserAccountWriteRepository
{
    public Task<UserAccount?> GetForUpdateAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .FirstOrDefaultAsync(
                userAccount =>
                    userAccount.Id == userAccountId,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
