using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequests;
using LeaveManagementSystem.Application.UnitTests.TestDoubles;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.LeaveRequests.Queries.GetLeaveRequests;

public sealed class GetLeaveRequestsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Employee_ReturnsOwnLeaveRequestsFromScopedRepository()
    {
        var employeeId =
            Guid.NewGuid();

        IReadOnlyList<LeaveRequestDto> expectedLeaveRequests =
        [
            CreateLeaveRequestDto()
        ];

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var scopedReadRepository =
            new FakeLeaveRequestScopedReadRepository
            {
                EmployeeLeaveRequests =
                    expectedLeaveRequests
            };

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId,
                EmployeeRole.Employee);

        var handler =
            new GetLeaveRequestsQueryHandler(
                readRepository,
                scopedReadRepository,
                currentUserAccessService);

        var result = await handler.Handle(
            new GetLeaveRequestsQuery(),
            CancellationToken.None);

        Assert.Same(
            expectedLeaveRequests,
            result);

        Assert.Equal(
            1,
            scopedReadRepository.GetAllForEmployeeCallCount);

        Assert.Equal(
            employeeId,
            scopedReadRepository.RequestedEmployeeId);

        Assert.Equal(
            0,
            scopedReadRepository.GetAllForManagerCallCount);

        Assert.Equal(
            0,
            readRepository.GetAllCallCount);
    }

    [Fact]
    public async Task Handle_Manager_ReturnsDirectReportLeaveRequestsFromScopedRepository()
    {
        var managerId =
            Guid.NewGuid();

        IReadOnlyList<LeaveRequestDto> expectedLeaveRequests =
        [
            CreateLeaveRequestDto()
        ];

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var scopedReadRepository =
            new FakeLeaveRequestScopedReadRepository
            {
                ManagerLeaveRequests =
                    expectedLeaveRequests
            };

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                managerId,
                EmployeeRole.Manager);

        var handler =
            new GetLeaveRequestsQueryHandler(
                readRepository,
                scopedReadRepository,
                currentUserAccessService);

        var result = await handler.Handle(
            new GetLeaveRequestsQuery(),
            CancellationToken.None);

        Assert.Same(
            expectedLeaveRequests,
            result);

        Assert.Equal(
            1,
            scopedReadRepository.GetAllForManagerCallCount);

        Assert.Equal(
            managerId,
            scopedReadRepository.RequestedManagerId);

        Assert.Equal(
            0,
            scopedReadRepository.GetAllForEmployeeCallCount);

        Assert.Equal(
            0,
            readRepository.GetAllCallCount);
    }

    [Fact]
    public async Task Handle_Hr_ReturnsAllLeaveRequestsFromReadRepository()
    {
        var hrEmployeeId =
            Guid.NewGuid();

        IReadOnlyList<LeaveRequestDto> expectedLeaveRequests =
        [
            CreateLeaveRequestDto()
        ];

        var readRepository =
            new FakeLeaveRequestReadRepository
            {
                LeaveRequests =
                    expectedLeaveRequests
            };

        var scopedReadRepository =
            new FakeLeaveRequestScopedReadRepository();

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                hrEmployeeId,
                EmployeeRole.HR);

        var handler =
            new GetLeaveRequestsQueryHandler(
                readRepository,
                scopedReadRepository,
                currentUserAccessService);

        var result = await handler.Handle(
            new GetLeaveRequestsQuery(),
            CancellationToken.None);

        Assert.Same(
            expectedLeaveRequests,
            result);

        Assert.Equal(
            1,
            readRepository.GetAllCallCount);

        Assert.Equal(
            0,
            scopedReadRepository.GetAllForEmployeeCallCount);

        Assert.Equal(
            0,
            scopedReadRepository.GetAllForManagerCallCount);
    }

    [Fact]
    public async Task Handle_CurrentUserAccessMissing_ThrowsForbiddenBeforeRepositoryCall()
    {
        var readRepository =
            new FakeLeaveRequestReadRepository();

        var scopedReadRepository =
            new FakeLeaveRequestScopedReadRepository();

        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result = null
            };

        var handler =
            new GetLeaveRequestsQueryHandler(
                readRepository,
                scopedReadRepository,
                currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new GetLeaveRequestsQuery(),
                    CancellationToken.None));

        Assert.Equal(
            "Only current active employees can access leave requests.",
            exception.Message);

        Assert.Equal(
            1,
            currentUserAccessService.CallCount);

        Assert.Equal(
            0,
            readRepository.GetAllCallCount);

        Assert.Equal(
            0,
            scopedReadRepository.GetAllForEmployeeCallCount);

        Assert.Equal(
            0,
            scopedReadRepository.GetAllForManagerCallCount);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToDependencies()
    {
        var employeeId =
            Guid.NewGuid();

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var scopedReadRepository =
            new FakeLeaveRequestScopedReadRepository();

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId,
                EmployeeRole.Employee);

        var handler =
            new GetLeaveRequestsQueryHandler(
                readRepository,
                scopedReadRepository,
                currentUserAccessService);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        await handler.Handle(
            new GetLeaveRequestsQuery(),
            cancellationToken);

        Assert.Equal(
            cancellationToken,
            currentUserAccessService
                .ReceivedCancellationToken);

        Assert.Equal(
            cancellationToken,
            scopedReadRepository
                .GetAllForEmployeeCancellationToken);
    }

    private static FakeCurrentUserAccessService
        CreateCurrentUserAccessService(
            Guid employeeId,
            EmployeeRole role)
    {
        return new FakeCurrentUserAccessService
        {
            Result =
                new CurrentUserAccess(
                    Guid.NewGuid(),
                    employeeId,
                    "irem@example.com",
                    role)
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

    private sealed class FakeLeaveRequestReadRepository
        : ILeaveRequestReadRepository
    {
        public IReadOnlyList<LeaveRequestDto> LeaveRequests
        {
            get;
            init;
        } = Array.Empty<LeaveRequestDto>();

        public int GetAllCallCount
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<LeaveRequestDto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            GetAllCallCount++;

            return Task.FromResult(
                LeaveRequests);
        }

        public Task<LeaveRequestDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }
    }

    private sealed class FakeLeaveRequestScopedReadRepository
        : ILeaveRequestScopedReadRepository
    {
        public IReadOnlyList<LeaveRequestDto> EmployeeLeaveRequests
        {
            get;
            init;
        } = Array.Empty<LeaveRequestDto>();

        public IReadOnlyList<LeaveRequestDto> ManagerLeaveRequests
        {
            get;
            init;
        } = Array.Empty<LeaveRequestDto>();

        public int GetAllForEmployeeCallCount
        {
            get;
            private set;
        }

        public int GetAllForManagerCallCount
        {
            get;
            private set;
        }

        public Guid RequestedEmployeeId
        {
            get;
            private set;
        }

        public Guid RequestedManagerId
        {
            get;
            private set;
        }

        public CancellationToken GetAllForEmployeeCancellationToken
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

            GetAllForEmployeeCancellationToken =
                cancellationToken;

            return Task.FromResult(
                EmployeeLeaveRequests);
        }

        public Task<IReadOnlyList<LeaveRequestDto>>
            GetAllForManagerAsync(
                Guid managerId,
                CancellationToken cancellationToken = default)
        {
            GetAllForManagerCallCount++;

            RequestedManagerId =
                managerId;

            return Task.FromResult(
                ManagerLeaveRequests);
        }

        public Task<LeaveRequestDto?> GetByIdForEmployeeAsync(
            Guid id,
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public Task<LeaveRequestDto?> GetByIdForManagerAsync(
            Guid id,
            Guid managerId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }
    }
}
