using LeaveManagementSystem.Application.Authentication.Models;

namespace LeaveManagementSystem.Application.Authentication.Abstractions;

public interface ICurrentUserAccessReadRepository
{
    Task<CurrentUserAccessData?> GetByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default);
}
