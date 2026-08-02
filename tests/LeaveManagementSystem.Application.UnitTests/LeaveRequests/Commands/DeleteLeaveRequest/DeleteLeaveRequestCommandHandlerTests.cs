using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Commands.DeleteLeaveRequest;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.LeaveRequests.Commands.DeleteLeaveRequest;

public sealed class DeleteLeaveRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_NullCommand_ThrowsBeforeRepositoryCalls()
    {
        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence);

        var handler =
            new DeleteLeaveRequestCommandHandler(
                writeRepository);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(
                null!,
                cancellationToken));

        Assert.Equal(
            0,
            writeRepository.GetForModificationCallCount);

        Assert.Equal(
            0,
            writeRepository.RemoveCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Empty(
            writeRepository.ReceivedCancellationTokens);

        Assert.Empty(
            callSequence);
    }

    [Fact]
    public async Task Handle_LeaveRequestDoesNotExist_ReturnsFalseAndStopsProcessing()
    {
        var leaveRequestId =
            Guid.NewGuid();

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    null
            };

        var handler =
            new DeleteLeaveRequestCommandHandler(
                writeRepository);

        var command =
            new DeleteLeaveRequestCommand(
                leaveRequestId);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var result =
            await handler.Handle(
                command,
                cancellationToken);

        Assert.False(
            result);

        Assert.Equal(
            1,
            writeRepository.GetForModificationCallCount);

        Assert.Equal(
            leaveRequestId,
            writeRepository.RequestedId);

        Assert.Equal(
            0,
            writeRepository.RemoveCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            cancellationToken,
            Assert.Single(
                writeRepository.ReceivedCancellationTokens));

        Assert.Equal(
            new[]
            {
                "GetForModification"
            },
            callSequence);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Approved)]
    [InlineData(LeaveRequestStatus.Rejected)]
    public async Task Handle_NonPendingLeaveRequest_ThrowsAndDoesNotDelete(
        LeaveRequestStatus status)
    {
        var leaveRequest =
            CreateLeaveRequest(
                status);

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    leaveRequest
            };

        var handler =
            new DeleteLeaveRequestCommandHandler(
                writeRepository);

        var command =
            new DeleteLeaveRequestCommand(
                leaveRequest.Id);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Only pending leave requests can be modified.",
            exception.Message);

        Assert.Equal(
            status,
            leaveRequest.Status);

        Assert.Equal(
            1,
            writeRepository.GetForModificationCallCount);

        Assert.Equal(
            0,
            writeRepository.RemoveCallCount);

        Assert.Null(
            writeRepository.RemovedLeaveRequest);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            new[]
            {
                "GetForModification"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_PendingLeaveRequest_RemovesSavesAndReturnsTrue()
    {
        var leaveRequest =
            CreateLeaveRequest();

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    leaveRequest,
                AllowRemove =
                    true,
                AllowSaveChanges =
                    true
            };

        var handler =
            new DeleteLeaveRequestCommandHandler(
                writeRepository);

        var command =
            new DeleteLeaveRequestCommand(
                leaveRequest.Id);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var result =
            await handler.Handle(
                command,
                cancellationToken);

        Assert.True(
            result);

        Assert.Equal(
            1,
            writeRepository.GetForModificationCallCount);

        Assert.Equal(
            leaveRequest.Id,
            writeRepository.RequestedId);

        Assert.Equal(
            1,
            writeRepository.RemoveCallCount);

        Assert.Same(
            leaveRequest,
            writeRepository.RemovedLeaveRequest);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            new[]
            {
                cancellationToken,
                cancellationToken
            },
            writeRepository.ReceivedCancellationTokens);

        Assert.Equal(
            new[]
            {
                "GetForModification",
                "Remove",
                "SaveChanges"
            },
            callSequence);
    }

    private static LeaveRequest CreateLeaveRequest(
        LeaveRequestStatus status =
            LeaveRequestStatus.Pending)
    {
        var leaveRequest =
            new LeaveRequest
            {
                EmployeeId =
                    Guid.NewGuid(),
                LeaveTypeId =
                    Guid.NewGuid(),
                Reason =
                    "Delete command unit test"
            };

        leaveRequest.SetDateRange(
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 12));

        switch (status)
        {
            case LeaveRequestStatus.Pending:
                break;

            case LeaveRequestStatus.Approved:
                leaveRequest.Approve(
                    Guid.NewGuid(),
                    "Approved for unit test.");
                break;

            case LeaveRequestStatus.Rejected:
                leaveRequest.Reject(
                    Guid.NewGuid(),
                    "Rejected for unit test.");
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Unsupported leave request status.");
        }

        return leaveRequest;
    }

    private sealed class FakeLeaveRequestWriteRepository
        : ILeaveRequestWriteRepository
    {
        private readonly List<string> callSequence;

        public FakeLeaveRequestWriteRepository(
            List<string> callSequence)
        {
            this.callSequence =
                callSequence;
        }

        public LeaveRequest? LeaveRequestResult
        {
            get;
            init;
        }

        public bool AllowRemove
        {
            get;
            init;
        }

        public bool AllowSaveChanges
        {
            get;
            init;
        }

        public Guid RequestedId
        {
            get;
            private set;
        }

        public int GetForModificationCallCount
        {
            get;
            private set;
        }

        public LeaveRequest? RemovedLeaveRequest
        {
            get;
            private set;
        }

        public int RemoveCallCount
        {
            get;
            private set;
        }

        public int SaveChangesCallCount
        {
            get;
            private set;
        }

        public List<CancellationToken>
            ReceivedCancellationTokens
        {
            get;
        } = new();

        public Task<LeaveRequest?> GetForModificationAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            GetForModificationCallCount++;

            RequestedId =
                id;

            ReceivedCancellationTokens.Add(
                cancellationToken);

            callSequence.Add(
                "GetForModification");

            return Task.FromResult(
                LeaveRequestResult);
        }

        public Task<bool> ActiveEmployeeExistsAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public Task<LeaveType?> GetLeaveTypeAsync(
            Guid leaveTypeId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public Task<bool> HasOverlappingLeaveRequestAsync(
            Guid employeeId,
            DateOnly startDate,
            DateOnly endDate,
            Guid? excludedLeaveRequestId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public Task<int> GetApprovedUsedDaysForYearAsync(
            Guid employeeId,
            Guid leaveTypeId,
            int year,
            Guid? excludedLeaveRequestId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public void Add(
            LeaveRequest leaveRequest)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public void Remove(
            LeaveRequest leaveRequest)
        {
            if (!AllowRemove)
            {
                throw new InvalidOperationException(
                    "Unexpected repository call.");
            }

            RemoveCallCount++;

            RemovedLeaveRequest =
                leaveRequest;

            callSequence.Add(
                "Remove");
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            if (!AllowSaveChanges)
            {
                throw new InvalidOperationException(
                    "Unexpected repository call.");
            }

            SaveChangesCallCount++;

            ReceivedCancellationTokens.Add(
                cancellationToken);

            callSequence.Add(
                "SaveChanges");

            return Task.CompletedTask;
        }
    }
}
