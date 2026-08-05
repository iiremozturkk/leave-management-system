using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Queries.GetMyLeaveRequests;
using LeaveManagementSystem.Application.UnitTests.TestDoubles;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.LeaveRequests.Queries.GetMyLeaveRequests;

public sealed class GetMyLeaveRequestsQueryHandlerTests
{
    [Theory]
    [InlineData(EmployeeRole.Employee)]
    [InlineData(EmployeeRole.Manager)]
    [InlineData(EmployeeRole.HR)]
    public async Task Handle_CurrentEmployeeRole_ReturnsOnlyCurrentEmployeesLeaveRequests(
        EmployeeRole role)
    {
        var employeeId =
            Guid.NewGuid();

        IReadOnlyList<LeaveRequestDto> expectedLeaveRequests =
        [
            CreateLeaveRequestDto(
                employeeId)
        ];

        var scopedReadRepository =
            new FakeLeaveRequestScopedReadRepository
            {
                EmployeeLeaveRequests =
                    expectedLeaveRequests
            };

        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result =
                    new CurrentUserAccess(
                        Guid.NewGuid(),
                        employeeId,
                        "current.employee@example.com",
                        role)
            };

        var handler =
            new GetMyLeaveRequestsQueryHandler(
                scopedReadRepository,
                currentUserAccessService);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var result = await handler.Handle(
            new GetMyLeaveRequestsQuery(),
            cancellationToken);

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
            cancellationToken,
            scopedReadRepository.ReceivedCancellationToken);

        Assert.Equal(
            cancellationToken,
            currentUserAccessService.ReceivedCancellationToken);
    }

    [Fact]
    public async Task Handle_CurrentUserAccessMissing_ThrowsForbiddenBeforeRepositoryCall()
    {
        var scopedReadRepository =
            new FakeLeaveRequestScopedReadRepository();

        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result = null
            };

        var handler =
            new GetMyLeaveRequestsQueryHandler(
                scopedReadRepository,
                currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new GetMyLeaveRequestsQuery(),
                    CancellationToken.None));

        Assert.Equal(
            "Only current active employees can access their leave requests.",
            exception.Message);

        Assert.Equal(
            1,
            currentUserAccessService.CallCount);

        Assert.Equal(
            0,
            scopedReadRepository.GetAllForEmployeeCallCount);
    }

    private static LeaveRequestDto CreateLeaveRequestDto(
        Guid employeeId)
    {
        return new LeaveRequestDto(
            Guid.NewGuid(),
            employeeId,
            "Current Employee",
            Guid.NewGuid(),
            "Annual Leave",
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 12),
            3,
            LeaveRequestStatus.Pending,
            "Own leave request.",
            null,
            null,
            null,
            null,
            DateTime.UtcNow,
            null);
    }

    private sealed class FakeLeaveRequestScopedReadRepository
        : ILeaveRequestScopedReadRepository
    {
        public IReadOnlyList<LeaveRequestDto> EmployeeLeaveRequests
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

        public CancellationToken ReceivedCancellationToken
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

            ReceivedCancellationToken =
                cancellationToken;

            return Task.FromResult(
                EmployeeLeaveRequests);
        }

        public Task<LeaveRequestDto?> GetByIdForEmployeeAsync(
            Guid id,
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
