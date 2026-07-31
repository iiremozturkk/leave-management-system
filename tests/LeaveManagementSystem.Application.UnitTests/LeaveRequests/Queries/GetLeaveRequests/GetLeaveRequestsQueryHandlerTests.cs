using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequests;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.LeaveRequests.Queries.GetLeaveRequests;

public sealed class GetLeaveRequestsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsLeaveRequestsFromRepository()
    {
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

        var handler =
            new GetLeaveRequestsQueryHandler(
                readRepository);

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
            readRepository.GetAllCallCount);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToRepository()
    {
        var readRepository =
            new FakeLeaveRequestReadRepository();

        var handler =
            new GetLeaveRequestsQueryHandler(
                readRepository);

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

        public CancellationToken GetAllCancellationToken
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<LeaveRequestDto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            GetAllCallCount++;
            GetAllCancellationToken =
                cancellationToken;

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
}
