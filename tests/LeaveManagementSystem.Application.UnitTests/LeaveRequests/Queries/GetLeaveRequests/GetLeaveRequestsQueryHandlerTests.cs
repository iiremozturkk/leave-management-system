using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequests;
using LeaveManagementSystem.Application.UnitTests.TestDoubles;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.LeaveRequests.Queries.GetLeaveRequests;

public sealed class GetLeaveRequestsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsLeaveRequestsFromRepository()
    {
        var employeeId =
            Guid.NewGuid();

        IReadOnlyList<LeaveRequestDto> expectedLeaveRequests =
        [
            CreateLeaveRequestDto()
        ];

        var readRepository =
            new FakeLeaveRequestSelfServiceReadRepository
            {
                LeaveRequests =
                    expectedLeaveRequests
            };

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId);

        var handler =
            new GetLeaveRequestsQueryHandler(
                readRepository,
                currentUserAccessService);

        var query =
            new GetLeaveRequestsQuery();

        var result = await handler.Handle(
            query,
            CancellationToken.None);

        Assert.Same(
            expectedLeaveRequests,
            result);

        Assert.Equal(
            1,
            readRepository.GetAllForEmployeeCallCount);

        Assert.Equal(
            employeeId,
            readRepository.RequestedEmployeeId);
    }

    [Fact]
    public async Task Handle_CurrentUserAccessMissing_ThrowsForbiddenBeforeRepositoryCall()
    {
        var readRepository =
            new FakeLeaveRequestSelfServiceReadRepository();

        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result = null
            };

        var handler =
            new GetLeaveRequestsQueryHandler(
                readRepository,
                currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new GetLeaveRequestsQuery(),
                    CancellationToken.None));

        Assert.Equal(
            "Only current active employees can use leave self-service operations.",
            exception.Message);

        Assert.Equal(
            1,
            currentUserAccessService.CallCount);

        Assert.Equal(
            0,
            readRepository.GetAllForEmployeeCallCount);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToRepository()
    {
        var employeeId =
            Guid.NewGuid();

        var readRepository =
            new FakeLeaveRequestSelfServiceReadRepository();

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId);

        var handler =
            new GetLeaveRequestsQueryHandler(
                readRepository,
                currentUserAccessService);

        var query =
            new GetLeaveRequestsQuery();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        await handler.Handle(
            query,
            cancellationToken);

        Assert.Equal(
            cancellationToken,
            readRepository.GetAllCancellationToken);

        Assert.Equal(
            cancellationToken,
            currentUserAccessService
                .ReceivedCancellationToken);
    }

    private static FakeCurrentUserAccessService
        CreateCurrentUserAccessService(
            Guid employeeId)
    {
        return new FakeCurrentUserAccessService
        {
            Result =
                new CurrentUserAccess(
                    Guid.NewGuid(),
                    employeeId,
                    "irem@example.com",
                    EmployeeRole.Employee)
        };
    }

    private static LeaveRequestDto CreateLeaveRequestDto()
    {
        return new LeaveRequestDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Irem Ozturk",
            Guid.NewGuid(),
            "Annual Leave",
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 12),
            3,
            LeaveRequestStatus.Pending,
            "Summer leave.",
            null,
            null,
            null,
            null,
            DateTime.UtcNow,
            null);
    }

    private sealed class FakeLeaveRequestSelfServiceReadRepository
        : ILeaveRequestSelfServiceReadRepository
    {
        public IReadOnlyList<LeaveRequestDto> LeaveRequests
        {
            get;
            init;
        } = Array.Empty<LeaveRequestDto>();

        public int GetAllForEmployeeCallCount
        {
            get;
            private set;
        }

        public Guid RequestedEmployeeId
        {
            get;
            private set;
        }

        public CancellationToken GetAllCancellationToken
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<LeaveRequestDto>>
            GetAllForEmployeeAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetAllForEmployeeCallCount++;

            RequestedEmployeeId =
                employeeId;

            GetAllCancellationToken =
                cancellationToken;

            return Task.FromResult(
                LeaveRequests);
        }

        public Task<LeaveRequestDto?> GetByIdForEmployeeAsync(
            Guid id,
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }
    }
}
