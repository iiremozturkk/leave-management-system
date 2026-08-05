using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;

namespace LeaveManagementSystem.Application.UnitTests.TestDoubles;

internal sealed class FakeCurrentUserAccessService
    : ICurrentUserAccessService
{
    public CurrentUserAccess? Result
    {
        get;
        init;
    }

    public int CallCount
    {
        get;
        private set;
    }

    public CancellationToken ReceivedCancellationToken
    {
        get;
        private set;
    }

    public Task<CurrentUserAccess?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        CallCount++;

        ReceivedCancellationToken =
            cancellationToken;

        return Task.FromResult(
            Result);
    }
}
