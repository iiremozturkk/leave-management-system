using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveBalance;
using LeaveManagementSystem.Application.UnitTests.TestDoubles;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.LeaveRequests.Queries.GetLeaveBalance;

public sealed class GetLeaveBalanceQueryHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_ReturnsBalanceFromRepository()
    {
        var employeeId =
            Guid.NewGuid();

        var leaveTypeId =
            Guid.NewGuid();

        var expectedBalance =
            CreateLeaveBalanceDto(
                employeeId,
                leaveTypeId,
                2026);

        var repository =
            new FakeLeaveBalanceReadRepository
            {
                Balance = expectedBalance
            };

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId);

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository,
                currentUserAccessService);

        var query =
            new GetLeaveBalanceQuery(
                leaveTypeId,
                2026);

        var result = await handler.Handle(
            query,
            CancellationToken.None);

        Assert.Same(
            expectedBalance,
            result);

        Assert.Equal(
            employeeId,
            repository.RequestedEmployeeId);

        Assert.Equal(
            leaveTypeId,
            repository.RequestedLeaveTypeId);

        Assert.Equal(
            2026,
            repository.RequestedYear);

        Assert.Null(
            repository.RequestedExcludedLeaveRequestId);

        Assert.Equal(
            1,
            repository.GetBalanceCallCount);
    }

    [Fact]
    public async Task Handle_RepositoryReturnsNull_ReturnsNull()
    {
        var employeeId =
            Guid.NewGuid();

        var leaveTypeId =
            Guid.NewGuid();

        var repository =
            new FakeLeaveBalanceReadRepository
            {
                Balance = null
            };

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId);

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository,
                currentUserAccessService);

        var query =
            new GetLeaveBalanceQuery(
                leaveTypeId,
                2026);

        var result = await handler.Handle(
            query,
            CancellationToken.None);

        Assert.Null(
            result);

        Assert.Equal(
            employeeId,
            repository.RequestedEmployeeId);

        Assert.Equal(
            1,
            repository.GetBalanceCallCount);
    }

    [Fact]
    public async Task Handle_CurrentUserAccessMissing_ThrowsForbiddenBeforeRepositoryCall()
    {
        var repository =
            new FakeLeaveBalanceReadRepository();

        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result = null
            };

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository,
                currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new GetLeaveBalanceQuery(
                        Guid.NewGuid(),
                        2026),
                    CancellationToken.None));

        Assert.Equal(
            "Only current active employees can use leave self-service operations.",
            exception.Message);

        Assert.Equal(
            1,
            currentUserAccessService.CallCount);

        Assert.Equal(
            0,
            repository.GetBalanceCallCount);
    }

    [Fact]
    public async Task Handle_LeaveTypeIdIsEmpty_ThrowsBeforeRepositoryCall()
    {
        var repository =
            new FakeLeaveBalanceReadRepository();

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                Guid.NewGuid());

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository,
                currentUserAccessService);

        var query =
            new GetLeaveBalanceQuery(
                Guid.Empty,
                2026);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    query,
                    CancellationToken.None));

        Assert.Equal(
            "Leave type id cannot be empty.",
            exception.Message);

        Assert.Equal(
            0,
            repository.GetBalanceCallCount);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public async Task Handle_YearIsOutsideSupportedRange_ThrowsBeforeRepositoryCall(
        int year)
    {
        var repository =
            new FakeLeaveBalanceReadRepository();

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                Guid.NewGuid());

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository,
                currentUserAccessService);

        var query =
            new GetLeaveBalanceQuery(
                Guid.NewGuid(),
                year);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    query,
                    CancellationToken.None));

        Assert.Equal(
            "Year must be between 2000 and 2100.",
            exception.Message);

        Assert.Equal(
            0,
            repository.GetBalanceCallCount);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2100)]
    public async Task Handle_YearIsSupportedBoundary_CallsRepository(
        int year)
    {
        var employeeId =
            Guid.NewGuid();

        var leaveTypeId =
            Guid.NewGuid();

        var repository =
            new FakeLeaveBalanceReadRepository();

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId);

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository,
                currentUserAccessService);

        var query =
            new GetLeaveBalanceQuery(
                leaveTypeId,
                year);

        await handler.Handle(
            query,
            CancellationToken.None);

        Assert.Equal(
            employeeId,
            repository.RequestedEmployeeId);

        Assert.Equal(
            year,
            repository.RequestedYear);

        Assert.Equal(
            1,
            repository.GetBalanceCallCount);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenAndNullExcludedIdToDependencies()
    {
        var employeeId =
            Guid.NewGuid();

        var leaveTypeId =
            Guid.NewGuid();

        var repository =
            new FakeLeaveBalanceReadRepository();

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId);

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository,
                currentUserAccessService);

        var query =
            new GetLeaveBalanceQuery(
                leaveTypeId,
                2026);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        await handler.Handle(
            query,
            cancellationToken);

        Assert.Equal(
            cancellationToken,
            currentUserAccessService
                .ReceivedCancellationToken);

        Assert.Equal(
            cancellationToken,
            repository.RequestedCancellationToken);

        Assert.Null(
            repository.RequestedExcludedLeaveRequestId);

        Assert.Equal(
            1,
            repository.GetBalanceCallCount);
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

    private static LeaveBalanceDto CreateLeaveBalanceDto(
        Guid employeeId,
        Guid leaveTypeId,
        int year)
    {
        return new LeaveBalanceDto(
            employeeId,
            leaveTypeId,
            "Annual Leave",
            year,
            20,
            5,
            15);
    }

    private sealed class FakeLeaveBalanceReadRepository
        : ILeaveBalanceReadRepository
    {
        public LeaveBalanceDto? Balance
        {
            get;
            init;
        }

        public Guid RequestedEmployeeId
        {
            get;
            private set;
        }

        public Guid RequestedLeaveTypeId
        {
            get;
            private set;
        }

        public int RequestedYear
        {
            get;
            private set;
        }

        public Guid? RequestedExcludedLeaveRequestId
        {
            get;
            private set;
        }

        public CancellationToken RequestedCancellationToken
        {
            get;
            private set;
        }

        public int GetBalanceCallCount
        {
            get;
            private set;
        }

        public Task<LeaveBalanceDto?> GetBalanceAsync(
            Guid employeeId,
            Guid leaveTypeId,
            int year,
            Guid? excludedLeaveRequestId = null,
            CancellationToken cancellationToken = default)
        {
            GetBalanceCallCount++;

            RequestedEmployeeId =
                employeeId;

            RequestedLeaveTypeId =
                leaveTypeId;

            RequestedYear =
                year;

            RequestedExcludedLeaveRequestId =
                excludedLeaveRequestId;

            RequestedCancellationToken =
                cancellationToken;

            return Task.FromResult(
                Balance);
        }
    }
}
