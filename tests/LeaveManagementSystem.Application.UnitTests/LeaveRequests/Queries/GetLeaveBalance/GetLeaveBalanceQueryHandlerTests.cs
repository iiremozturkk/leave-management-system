using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveBalance;
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

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository);

        var query =
            new GetLeaveBalanceQuery(
                employeeId,
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

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository);

        var query =
            new GetLeaveBalanceQuery(
                employeeId,
                leaveTypeId,
                2026);

        var result = await handler.Handle(
            query,
            CancellationToken.None);

        Assert.Null(
            result);

        Assert.Equal(
            1,
            repository.GetBalanceCallCount);
    }

    [Fact]
    public async Task Handle_EmployeeIdIsEmpty_ThrowsBeforeRepositoryCall()
    {
        var repository =
            new FakeLeaveBalanceReadRepository();

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository);

        var query =
            new GetLeaveBalanceQuery(
                Guid.Empty,
                Guid.NewGuid(),
                2026);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    query,
                    CancellationToken.None));

        Assert.Equal(
            "Employee id cannot be empty.",
            exception.Message);

        Assert.Equal(
            0,
            repository.GetBalanceCallCount);
    }

    [Fact]
    public async Task Handle_LeaveTypeIdIsEmpty_ThrowsBeforeRepositoryCall()
    {
        var repository =
            new FakeLeaveBalanceReadRepository();

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository);

        var query =
            new GetLeaveBalanceQuery(
                Guid.NewGuid(),
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

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository);

        var query =
            new GetLeaveBalanceQuery(
                Guid.NewGuid(),
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

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository);

        var query =
            new GetLeaveBalanceQuery(
                employeeId,
                leaveTypeId,
                year);

        await handler.Handle(
            query,
            CancellationToken.None);

        Assert.Equal(
            year,
            repository.RequestedYear);

        Assert.Equal(
            1,
            repository.GetBalanceCallCount);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenAndNullExcludedIdToRepository()
    {
        var employeeId =
            Guid.NewGuid();

        var leaveTypeId =
            Guid.NewGuid();

        var repository =
            new FakeLeaveBalanceReadRepository();

        var handler =
            new GetLeaveBalanceQueryHandler(
                repository);

        var query =
            new GetLeaveBalanceQuery(
                employeeId,
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
            repository.RequestedCancellationToken);

        Assert.Null(
            repository.RequestedExcludedLeaveRequestId);

        Assert.Equal(
            1,
            repository.GetBalanceCallCount);
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
