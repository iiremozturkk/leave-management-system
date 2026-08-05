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
    public async Task Handle_LeaveRequestExists_ReturnsLeaveRequestFromRepository()
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
            new FakeLeaveRequestSelfServiceReadRepository
            {
                LeaveRequestById =
                    expectedLeaveRequest
            };

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId);

        var handler =
            new GetLeaveRequestByIdQueryHandler(
                readRepository,
                currentUserAccessService);

        var query =
            new GetLeaveRequestByIdQuery(
                leaveRequestId);

        var result = await handler.Handle(
            query,
            CancellationToken.None);

        Assert.Same(
            expectedLeaveRequest,
            result);

        Assert.Equal(
            leaveRequestId,
            readRepository.RequestedLeaveRequestId);

        Assert.Equal(
            employeeId,
            readRepository.RequestedEmployeeId);

        Assert.Equal(
            1,
            readRepository.GetByIdForEmployeeCallCount);
    }

    [Fact]
    public async Task Handle_LeaveRequestDoesNotExist_ReturnsNull()
    {
        var leaveRequestId =
            Guid.NewGuid();

        var employeeId =
            Guid.NewGuid();

        var readRepository =
            new FakeLeaveRequestSelfServiceReadRepository
            {
                LeaveRequestById = null
            };

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId);

        var handler =
            new GetLeaveRequestByIdQueryHandler(
                readRepository,
                currentUserAccessService);

        var query =
            new GetLeaveRequestByIdQuery(
                leaveRequestId);

        var result = await handler.Handle(
            query,
            CancellationToken.None);

        Assert.Null(
            result);

        Assert.Equal(
            leaveRequestId,
            readRepository.RequestedLeaveRequestId);

        Assert.Equal(
            employeeId,
            readRepository.RequestedEmployeeId);

        Assert.Equal(
            1,
            readRepository.GetByIdForEmployeeCallCount);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToDependencies()
    {
        var leaveRequestId =
            Guid.NewGuid();

        var employeeId =
            Guid.NewGuid();

        var readRepository =
            new FakeLeaveRequestSelfServiceReadRepository();

        var currentUserAccessService =
            CreateCurrentUserAccessService(
                employeeId);

        var handler =
            new GetLeaveRequestByIdQueryHandler(
                readRepository,
                currentUserAccessService);

        var query =
            new GetLeaveRequestByIdQuery(
                leaveRequestId);

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
            readRepository.GetByIdCancellationToken);
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
            new GetLeaveRequestByIdQueryHandler(
                readRepository,
                currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new GetLeaveRequestByIdQuery(
                        Guid.NewGuid()),
                    CancellationToken.None));

        Assert.Equal(
            "Only current active employees can use leave self-service operations.",
            exception.Message);

        Assert.Equal(
            1,
            currentUserAccessService.CallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdForEmployeeCallCount);
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

    private sealed class FakeLeaveRequestSelfServiceReadRepository
        : ILeaveRequestSelfServiceReadRepository
    {
        public LeaveRequestDto? LeaveRequestById
        {
            get;
            init;
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

        public int GetByIdForEmployeeCallCount
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
    }
}
