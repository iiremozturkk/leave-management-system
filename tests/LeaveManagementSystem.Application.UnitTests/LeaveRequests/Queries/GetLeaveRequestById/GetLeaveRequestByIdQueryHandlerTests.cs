using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequestById;
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

        var expectedLeaveRequest =
            CreateLeaveRequestDto(
                leaveRequestId);

        var readRepository =
            new FakeLeaveRequestReadRepository
            {
                LeaveRequestById =
                    expectedLeaveRequest
            };

        var handler =
            new GetLeaveRequestByIdQueryHandler(
                readRepository);

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
            1,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_LeaveRequestDoesNotExist_ReturnsNull()
    {
        var leaveRequestId =
            Guid.NewGuid();

        var readRepository =
            new FakeLeaveRequestReadRepository
            {
                LeaveRequestById = null
            };

        var handler =
            new GetLeaveRequestByIdQueryHandler(
                readRepository);

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
            1,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToRepository()
    {
        var leaveRequestId =
            Guid.NewGuid();

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var handler =
            new GetLeaveRequestByIdQueryHandler(
                readRepository);

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
            leaveRequestId,
            readRepository.RequestedLeaveRequestId);

        Assert.Equal(
            cancellationToken,
            readRepository.GetByIdCancellationToken);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);
    }

    private static LeaveRequestDto CreateLeaveRequestDto(
        Guid leaveRequestId)
    {
        return new LeaveRequestDto(
            leaveRequestId,
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

        public int GetByIdCallCount
        {
            get;
            private set;
        }

        public CancellationToken GetByIdCancellationToken
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
            RequestedLeaveRequestId = id;
            GetByIdCancellationToken =
                cancellationToken;

            return Task.FromResult(
                LeaveRequestById);
        }
    }
}
