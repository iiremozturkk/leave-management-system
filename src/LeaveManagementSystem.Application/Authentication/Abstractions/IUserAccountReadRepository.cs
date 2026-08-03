using LeaveManagementSystem.Application.Authentication.Models;

namespace LeaveManagementSystem.Application.Authentication.Abstractions;

public interface IUserAccountReadRepository
{
    Task<UserAccountAuthenticationData?> GetByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);
}
