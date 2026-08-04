using LeaveManagementSystem.Application.Authentication.Models;

namespace LeaveManagementSystem.Application.Authentication.Abstractions;

public interface ICurrentUserAccessService
{
    Task<CurrentUserAccess?> GetAsync(
        CancellationToken cancellationToken = default);
}
