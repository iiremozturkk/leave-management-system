using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequestById;
using LeaveManagementSystem.Application.UnitTests.TestDoubles;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.LeaveRequests.Queries.GetLeaveRequestById;

public sealed class GetLeaveRequestByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_Employee_ReturnsLeaveRequestFromEmployeeScope()
    {
        var leaveRequestId =
            Guid.NewGuid();

        var employeeId =
            Guid.NewGuid();

        var expectedLeaveRequest =
            CreateLeaveRequestDto(
                leaveRequestId,
                employeeId);

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var scopedReadRepository =
            new FakeLeaveRequestScopedReadRepository
            {
                LeaveRequestById =
                    expectedLeaveRequest
            };

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId,
                EmployeeRole.Employee);

        var handler =
            new GetLeaveRequestByIdQueryHandler(
                readRepository,
                scopedReadRepository,
                currentUserAccessService);

        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(
                leaveRequestId),
            CancellationToken.None);

        Assert.Same(
            expectedLeaveRequest,
            result);

        Assert.Equal(
            1,
            scopedReadRepository
                .GetByIdForEmployeeCallCount);

        Assert.Equal(
            leaveRequestId,
            scopedReadRepository
                .RequestedLeaveRequestId);

        Assert.Equal(
            employeeId,
            scopedReadRepository
                .RequestedEmployeeId);

        Assert.Equal(
            0,
            scopedReadRepository
                .GetByIdForManagerCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_Manager_ReturnsLeaveRequestFromManagerScope()
    {
        var leaveRequestId =
            Guid.NewGuid();

        var managerId =
            Guid.NewGuid();

        var expectedLeaveRequest =
            CreateLeaveRequestDto(
                leaveRequestId,
                Guid.NewGuid());

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var scopedReadRepository =
            new FakeLeaveRequestScopedReadRepository
            {
                LeaveRequestById =
                    expectedLeaveRequest
            };

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                managerId,
                EmployeeRole.Manager);

        var handler =
            new GetLeaveRequestByIdQueryHandler(
                readRepository,
                scopedReadRepository,
                currentUserAccessService);

        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(
                leaveRequestId),
            CancellationToken.None);

        Assert.Same(
            expectedLeaveRequest,
            result);

        Assert.Equal(
            1,
            scopedReadRepository
                .GetByIdForManagerCallCount);

        Assert.Equal(
            leaveRequestId,
            scopedReadRepository
                .RequestedLeaveRequestId);

        Assert.Equal(
            managerId,
            scopedReadRepository
                .RequestedManagerId);

        Assert.Equal(
            0,
            scopedReadRepository
                .GetByIdForEmployeeCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_Hr_ReturnsLeaveRequestFromUnrestrictedRepository()
    {
        var leaveRequestId =
            Guid.NewGuid();

        var hrEmployeeId =
            Guid.NewGuid();

        var expectedLeaveRequest =
            CreateLeaveRequestDto(
                leaveRequestId,
                Guid.NewGuid());

        var readRepository =
            new FakeLeaveRequestReadRepository
            {
                LeaveRequestById =
                    expectedLeaveRequest
            };

        var scopedReadRepository =
            new FakeLeaveRequestScopedReadRepository();

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                hrEmployeeId,
                EmployeeRole.HR);

        var handler =
            new GetLeaveRequestByIdQueryHandler(
                readRepository,
                scopedReadRepository,
                currentUserAccessService);

        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(
                leaveRequestId),
            CancellationToken.None);

        Assert.Same(
            expectedLeaveRequest,
            result);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);

        Assert.Equal(
            leaveRequestId,
            readRepository.RequestedLeaveRequestId);

        Assert.Equal(
            0,
            scopedReadRepository
                .GetByIdForEmployeeCallCount);

        Assert.Equal(
            0,
            scopedReadRepository
                .GetByIdForManagerCallCount);
    }

    [Fact]
    public async Task Handle_ScopedLeaveRequestDoesNotExist_ReturnsNull()
    {
        var leaveRequestId =
            Guid.NewGuid();

        var employeeId =
            Guid.NewGuid();

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var scopedReadRepository =
            new FakeLeaveRequestScopedReadRepository
            {
                LeaveRequestById = null
            };

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId,
                EmployeeRole.Employee);

        var handler =
            new GetLeaveRequestByIdQueryHandler(
                readRepository,
                scopedReadRepository,
                currentUserAccessService);

        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(
                leaveRequestId),
            CancellationToken.None);

        Assert.Null(
            result);

        Assert.Equal(
            1,
            scopedReadRepository
                .GetByIdForEmployeeCallCount);

        Assert.Equal(
            leaveRequestId,
            scopedReadRepository
                .RequestedLeaveRequestId);

        Assert.Equal(
            employeeId,
            scopedReadRepository
                .RequestedEmployeeId);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToDependencies()
    {
        var leaveRequestId =
            Guid.NewGuid();

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
            new GetLeaveRequestByIdQueryHandler(
                readRepository,
                scopedReadRepository,
                currentUserAccessService);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        await handler.Handle(
            new GetLeaveRequestByIdQuery(
                leaveRequestId),
            cancellationToken);

        Assert.Equal(
            cancellationToken,
            currentUserAccessService
                .ReceivedCancellationToken);

        Assert.Equal(
            cancellationToken,
            scopedReadRepository
                .GetByIdCancellationToken);
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
            new GetLeaveRequestByIdQueryHandler(
                readRepository,
                scopedReadRepository,
                currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new GetLeaveRequestByIdQuery(
                        Guid.NewGuid()),
                    CancellationToken.None));

        Assert.Equal(
            "Only current active employees can access leave requests.",
            exception.Message);

        Assert.Equal(
            1,
            currentUserAccessService.CallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);

        Assert.Equal(
            0,
            scopedReadRepository
                .GetByIdForEmployeeCallCount);

        Assert.Equal(
            0,
            scopedReadRepository
                .GetByIdForManagerCallCount);
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

    private static LeaveRequestDto CreateLeaveRequestDto(
        Guid leaveRequestId,
        Guid employeeId)
    {
        return new LeaveRequestDto(
            leaveRequestId,
            employeeId,
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
        public LeaveRequestDto? LeaveRequestById
        {
            get;
            init;
        }

        public int GetByIdCallCount
        {
            get;
            private set;
        }

        public Guid RequestedLeaveRequestId
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<LeaveRequestDto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public Task<LeaveRequestDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            RequestedLeaveRequestId =
                id;

            return Task.FromResult(
                LeaveRequestById);
        }
    }

    private sealed class FakeLeaveRequestScopedReadRepository
        : ILeaveRequestScopedReadRepository
    {
        public LeaveRequestDto? LeaveRequestById
        {
            get;
            init;
        }

        public int GetByIdForEmployeeCallCount
        {
            get;
            private set;
        }

        public int GetByIdForManagerCallCount
        {
            get;
            private set;
        }

        public Guid RequestedLeaveRequestId
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

        public CancellationToken GetByIdCancellationToken
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<LeaveRequestDto>>
            GetAllForEmployeeAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public Task<IReadOnlyList<LeaveRequestDto>>
            GetAllForManagerAsync(
                Guid managerId,
                CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public Task<LeaveRequestDto?> GetByIdForEmployeeAsync(
            Guid id,
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            GetByIdForEmployeeCallCount++;

            RequestedLeaveRequestId =
                id;

            RequestedEmployeeId =
                employeeId;

            GetByIdCancellationToken =
                cancellationToken;

            return Task.FromResult(
                LeaveRequestById);
        }

        public Task<LeaveRequestDto?> GetByIdForManagerAsync(
            Guid id,
            Guid managerId,
            CancellationToken cancellationToken = default)
        {
            GetByIdForManagerCallCount++;

            RequestedLeaveRequestId =
                id;

            RequestedManagerId =
                managerId;

            GetByIdCancellationToken =
                cancellationToken;

            return Task.FromResult(
                LeaveRequestById);
        }
    }
}
