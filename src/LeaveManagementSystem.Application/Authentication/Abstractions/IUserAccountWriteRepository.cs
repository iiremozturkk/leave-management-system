using LeaveManagementSystem.Domain.Entities;

namespace LeaveManagementSystem.Application.Authentication.Abstractions;

public interface IUserAccountWriteRepository
{
    Task<UserAccount?> GetForUpdateAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
