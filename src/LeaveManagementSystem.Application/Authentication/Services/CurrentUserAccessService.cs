using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Application.Authentication.Services;

public sealed class CurrentUserAccessService
    : ICurrentUserAccessService,
      IDisposable
{
    private readonly ICurrentUser _currentUser;

    private readonly ICurrentUserAccessReadRepository
        _currentUserAccessReadRepository;

    private readonly SemaphoreSlim _resolutionGate =
        new(
            initialCount: 1,
            maxCount: 1);

    private bool _hasCachedResolution;

    private CurrentUserAccess? _cachedResolution;

    public CurrentUserAccessService(
        ICurrentUser currentUser,
        ICurrentUserAccessReadRepository
            currentUserAccessReadRepository)
    {
        _currentUser =
            currentUser
            ?? throw new ArgumentNullException(
                nameof(currentUser));

        _currentUserAccessReadRepository =
            currentUserAccessReadRepository
            ?? throw new ArgumentNullException(
                nameof(currentUserAccessReadRepository));
    }

    public async Task<CurrentUserAccess?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        await _resolutionGate.WaitAsync(
            cancellationToken);

        try
        {
            if (_hasCachedResolution)
            {
                return _cachedResolution;
            }

            var resolution =
                await ResolveAsync(
                    cancellationToken);

            _cachedResolution =
                resolution;

            _hasCachedResolution =
                true;

            return resolution;
        }
        finally
        {
            _resolutionGate.Release();
        }
    }

    private async Task<CurrentUserAccess?> ResolveAsync(
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserAccountId
                is not Guid claimedUserAccountId
            || _currentUser.EmployeeId
                is not Guid claimedEmployeeId
            || _currentUser.Role
                is not EmployeeRole claimedRole
            || string.IsNullOrWhiteSpace(
                _currentUser.Email))
        {
            return null;
        }

        var accessData =
            await _currentUserAccessReadRepository
                .GetByUserAccountIdAsync(
                    claimedUserAccountId,
                    cancellationToken);

        if (accessData is null)
        {
            return null;
        }

        if (accessData.UserAccountId
                != claimedUserAccountId
            || accessData.EmployeeId
                != claimedEmployeeId
            || !accessData.IsUserAccountActive
            || !accessData.IsEmployeeActive
            || accessData.Role
                != claimedRole)
        {
            return null;
        }

        return new CurrentUserAccess(
            accessData.UserAccountId,
            accessData.EmployeeId,
            accessData.Email,
            accessData.Role);
    }

    public void Dispose()
    {
        _resolutionGate.Dispose();
    }
}
