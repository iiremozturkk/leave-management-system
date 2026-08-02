using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Commands.UpdateLeaveRequest;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.LeaveRequests.Commands.UpdateLeaveRequest;

public sealed class UpdateLeaveRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var writeRepository =
            new FakeLeaveRequestWriteRepository();

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(
                null!,
                CancellationToken.None));

        Assert.Equal(
            0,
            writeRepository.GetForUpdateCallCount);
    }

    [Fact]
    public async Task Handle_LeaveRequestDoesNotExist_ReturnsNullAndStopsProcessing()
    {
        var leaveRequestId =
            Guid.NewGuid();

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence);

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
                leaveRequestId);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var result =
            await handler.Handle(
                command,
                cancellationToken);

        Assert.Null(
            result);

        Assert.Equal(
            1,
            writeRepository.GetForUpdateCallCount);

        Assert.Equal(
            leaveRequestId,
            writeRepository.RequestedId);

        Assert.Equal(
            cancellationToken,
            Assert.Single(
                writeRepository.ReceivedCancellationTokens));

        Assert.Equal(
            new[]
            {
                "GetForUpdate"
            },
            callSequence);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Approved)]
    [InlineData(LeaveRequestStatus.Rejected)]
    public async Task Handle_NonPendingLeaveRequest_ThrowsAndStopsProcessing(
        LeaveRequestStatus status)
    {
        var leaveRequest =
            CreateLeaveRequest(
                status);

        var originalLeaveTypeId =
            leaveRequest.LeaveTypeId;

        var originalStartDate =
            leaveRequest.StartDate;

        var originalEndDate =
            leaveRequest.EndDate;

        var originalRequestedDays =
            leaveRequest.RequestedDays;

        var originalUpdatedAtUtc =
            leaveRequest.UpdatedAtUtc;

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    leaveRequest
            };

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
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
            new[]
            {
                "GetForUpdate"
            },
            callSequence);

        Assert.Equal(
            originalLeaveTypeId,
            leaveRequest.LeaveTypeId);

        Assert.Equal(
            originalStartDate,
            leaveRequest.StartDate);

        Assert.Equal(
            originalEndDate,
            leaveRequest.EndDate);

        Assert.Equal(
            originalRequestedDays,
            leaveRequest.RequestedDays);

        Assert.Equal(
            "Original reason",
            leaveRequest.Reason);

        Assert.Equal(
            originalUpdatedAtUtc,
            leaveRequest.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_EmptyReason_ThrowsBeforeFurtherRepositoryCalls()
    {
        var leaveRequest =
            CreateLeaveRequest();

        var originalLeaveTypeId =
            leaveRequest.LeaveTypeId;

        var originalStartDate =
            leaveRequest.StartDate;

        var originalEndDate =
            leaveRequest.EndDate;

        var originalRequestedDays =
            leaveRequest.RequestedDays;

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    leaveRequest
            };

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                Guid.NewGuid(),
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12),
                "   ");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Reason cannot be empty.",
            exception.Message);

        Assert.Equal(
            new[]
            {
                "GetForUpdate"
            },
            callSequence);

        Assert.Equal(
            originalLeaveTypeId,
            leaveRequest.LeaveTypeId);

        Assert.Equal(
            originalStartDate,
            leaveRequest.StartDate);

        Assert.Equal(
            originalEndDate,
            leaveRequest.EndDate);

        Assert.Equal(
            originalRequestedDays,
            leaveRequest.RequestedDays);

        Assert.Equal(
            "Original reason",
            leaveRequest.Reason);

        Assert.Null(
            leaveRequest.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_ReasonExceedsMaximumLength_ThrowsBeforeFurtherRepositoryCalls()
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
                    leaveRequest
            };

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                Guid.NewGuid(),
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12),
                new string(
                    'x',
                    501));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Reason cannot exceed 500 characters.",
            exception.Message);

        Assert.Equal(
            new[]
            {
                "GetForUpdate"
            },
            callSequence);

        Assert.Equal(
            0,
            writeRepository.GetLeaveTypeCallCount);
    }

    [Fact]
    public async Task Handle_EndDateEarlierThanStartDate_ThrowsBeforeLeaveTypeLookup()
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
                    leaveRequest
            };

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                Guid.NewGuid(),
                new DateOnly(2026, 7, 12),
                new DateOnly(2026, 7, 10),
                "Updated reason");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "End date cannot be earlier than start date.",
            exception.Message);

        Assert.Equal(
            new[]
            {
                "GetForUpdate"
            },
            callSequence);

        Assert.Equal(
            0,
            writeRepository.GetLeaveTypeCallCount);
    }

    [Theory]
    [InlineData(1999, 2000)]
    [InlineData(2100, 2101)]
    public async Task Handle_UnsupportedYear_ThrowsBeforeLeaveTypeLookup(
        int startYear,
        int endYear)
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
                    leaveRequest
            };

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                Guid.NewGuid(),
                new DateOnly(
                    startYear,
                    12,
                    31),
                new DateOnly(
                    endYear,
                    1,
                    1),
                "Updated reason");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Year must be between 2000 and 2100.",
            exception.Message);

        Assert.Equal(
            new[]
            {
                "GetForUpdate"
            },
            callSequence);

        Assert.Equal(
            0,
            writeRepository.GetLeaveTypeCallCount);
    }

    [Fact]
    public async Task Handle_EmptyLeaveTypeId_ThrowsBeforeLeaveTypeLookup()
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
                    leaveRequest
            };

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                Guid.Empty,
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12),
                "Updated reason");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Leave type id cannot be empty.",
            exception.Message);

        Assert.Equal(
            new[]
            {
                "GetForUpdate"
            },
            callSequence);

        Assert.Equal(
            0,
            writeRepository.GetLeaveTypeCallCount);
    }

    [Fact]
    public async Task Handle_LeaveTypeDoesNotExist_ThrowsAndStopsProcessing()
    {
        var leaveRequest =
            CreateLeaveRequest();

        var requestedLeaveTypeId =
            Guid.NewGuid();

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    leaveRequest,
                LeaveTypeResult =
                    null
            };

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                requestedLeaveTypeId,
                new DateOnly(2026, 7, 10),
                new DateOnly(2026, 7, 12),
                "Updated reason");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Leave type does not exist.",
            exception.Message);

        Assert.Equal(
            requestedLeaveTypeId,
            writeRepository.RequestedLeaveTypeId);

        Assert.Equal(
            1,
            writeRepository.GetLeaveTypeCallCount);

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
                "GetForUpdate",
                "GetLeaveType"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_OverlappingLeaveRequest_ThrowsAndExcludesCurrentRequest()
    {
        var leaveRequest =
            CreateLeaveRequest();

        var leaveType =
            CreateLeaveType();

        var requestedStartDate =
            new DateOnly(
                2026,
                7,
                10);

        var requestedEndDate =
            new DateOnly(
                2026,
                7,
                12);

        var originalLeaveTypeId =
            leaveRequest.LeaveTypeId;

        var originalStartDate =
            leaveRequest.StartDate;

        var originalEndDate =
            leaveRequest.EndDate;

        var originalRequestedDays =
            leaveRequest.RequestedDays;

        var originalUpdatedAtUtc =
            leaveRequest.UpdatedAtUtc;

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    leaveRequest,
                LeaveTypeResult =
                    leaveType,
                HasOverlapResult =
                    true
            };

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                leaveType.Id,
                requestedStartDate,
                requestedEndDate,
                "Updated reason");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Employee already has a leave request in the selected date range.",
            exception.Message);

        Assert.Equal(
            1,
            writeRepository.HasOverlapCallCount);

        Assert.Equal(
            leaveRequest.EmployeeId,
            writeRepository.RequestedOverlapEmployeeId);

        Assert.Equal(
            requestedStartDate,
            writeRepository.RequestedOverlapStartDate);

        Assert.Equal(
            requestedEndDate,
            writeRepository.RequestedOverlapEndDate);

        Assert.Equal(
            leaveRequest.Id,
            writeRepository.RequestedOverlapExcludedLeaveRequestId);

        Assert.Equal(
            new[]
            {
            cancellationToken,
            cancellationToken,
            cancellationToken
            },
            writeRepository.ReceivedCancellationTokens);

        Assert.Equal(
            new[]
            {
            "GetForUpdate",
            "GetLeaveType",
            "HasOverlap"
            },
            callSequence);

        Assert.Equal(
            originalLeaveTypeId,
            leaveRequest.LeaveTypeId);

        Assert.Equal(
            originalStartDate,
            leaveRequest.StartDate);

        Assert.Equal(
            originalEndDate,
            leaveRequest.EndDate);

        Assert.Equal(
            originalRequestedDays,
            leaveRequest.RequestedDays);

        Assert.Equal(
            "Original reason",
            leaveRequest.Reason);

        Assert.Equal(
            originalUpdatedAtUtc,
            leaveRequest.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_InsufficientBalance_ThrowsAndExcludesCurrentRequest()
    {
        var leaveRequest =
            CreateLeaveRequest();

        var leaveType =
            CreateLeaveType(
                defaultAnnualAllowanceDays: 20);

        var requestedStartDate =
            new DateOnly(
                2026,
                7,
                10);

        var requestedEndDate =
            new DateOnly(
                2026,
                7,
                12);

        var originalLeaveTypeId =
            leaveRequest.LeaveTypeId;

        var originalStartDate =
            leaveRequest.StartDate;

        var originalEndDate =
            leaveRequest.EndDate;

        var originalRequestedDays =
            leaveRequest.RequestedDays;

        var originalUpdatedAtUtc =
            leaveRequest.UpdatedAtUtc;

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    leaveRequest,
                LeaveTypeResult =
                    leaveType,
                HasOverlapResult =
                    false
            };

        writeRepository.ApprovedUsedDaysByYear[2026] =
            18;

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                leaveType.Id,
                requestedStartDate,
                requestedEndDate,
                "Updated reason");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Requested leave days exceed the remaining leave balance.",
            exception.Message);

        Assert.Equal(
            1,
            writeRepository.GetApprovedUsedDaysCallCount);

        var approvedUsedDaysRequest =
            Assert.Single(
                writeRepository.ApprovedUsedDaysRequests);

        Assert.Equal(
            leaveRequest.EmployeeId,
            approvedUsedDaysRequest.EmployeeId);

        Assert.Equal(
            leaveType.Id,
            approvedUsedDaysRequest.LeaveTypeId);

        Assert.Equal(
            2026,
            approvedUsedDaysRequest.Year);

        Assert.Equal(
            leaveRequest.Id,
            approvedUsedDaysRequest.ExcludedLeaveRequestId);

        Assert.Equal(
            new[]
            {
            cancellationToken,
            cancellationToken,
            cancellationToken,
            cancellationToken
            },
            writeRepository.ReceivedCancellationTokens);

        Assert.Equal(
            new[]
            {
            "GetForUpdate",
            "GetLeaveType",
            "HasOverlap",
            "GetApprovedUsedDaysForYear:2026"
            },
            callSequence);

        Assert.Equal(
            originalLeaveTypeId,
            leaveRequest.LeaveTypeId);

        Assert.Equal(
            originalStartDate,
            leaveRequest.StartDate);

        Assert.Equal(
            originalEndDate,
            leaveRequest.EndDate);

        Assert.Equal(
            originalRequestedDays,
            leaveRequest.RequestedDays);

        Assert.Equal(
            "Original reason",
            leaveRequest.Reason);

        Assert.Equal(
            originalUpdatedAtUtc,
            leaveRequest.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_CrossYearRequest_ValidatesEachYearAndThrowsWhenSecondYearBalanceIsInsufficient()
    {
        var leaveRequest =
            CreateLeaveRequest();

        var leaveType =
            CreateLeaveType(
                defaultAnnualAllowanceDays: 20);

        var requestedStartDate =
            new DateOnly(
                2026,
                12,
                30);

        var requestedEndDate =
            new DateOnly(
                2027,
                1,
                3);

        var originalLeaveTypeId =
            leaveRequest.LeaveTypeId;

        var originalStartDate =
            leaveRequest.StartDate;

        var originalEndDate =
            leaveRequest.EndDate;

        var originalRequestedDays =
            leaveRequest.RequestedDays;

        var originalUpdatedAtUtc =
            leaveRequest.UpdatedAtUtc;

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    leaveRequest,
                LeaveTypeResult =
                    leaveType,
                HasOverlapResult =
                    false
            };

        writeRepository.ApprovedUsedDaysByYear[2026] =
            18;

        writeRepository.ApprovedUsedDaysByYear[2027] =
            18;

        var readRepository =
            new FailFastLeaveRequestReadRepository();

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                leaveType.Id,
                requestedStartDate,
                requestedEndDate,
                "Cross-year updated reason");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    cancellationToken));

        Assert.Equal(
            "Requested leave days exceed the remaining leave balance.",
            exception.Message);

        Assert.Equal(
            2,
            writeRepository.GetApprovedUsedDaysCallCount);

        Assert.Equal(
            2,
            writeRepository.ApprovedUsedDaysRequests.Count);

        var firstYearRequest =
            writeRepository.ApprovedUsedDaysRequests[0];

        Assert.Equal(
            leaveRequest.EmployeeId,
            firstYearRequest.EmployeeId);

        Assert.Equal(
            leaveType.Id,
            firstYearRequest.LeaveTypeId);

        Assert.Equal(
            2026,
            firstYearRequest.Year);

        Assert.Equal(
            leaveRequest.Id,
            firstYearRequest.ExcludedLeaveRequestId);

        var secondYearRequest =
            writeRepository.ApprovedUsedDaysRequests[1];

        Assert.Equal(
            leaveRequest.EmployeeId,
            secondYearRequest.EmployeeId);

        Assert.Equal(
            leaveType.Id,
            secondYearRequest.LeaveTypeId);

        Assert.Equal(
            2027,
            secondYearRequest.Year);

        Assert.Equal(
            leaveRequest.Id,
            secondYearRequest.ExcludedLeaveRequestId);

        Assert.Equal(
            new[]
            {
            cancellationToken,
            cancellationToken,
            cancellationToken,
            cancellationToken,
            cancellationToken
            },
            writeRepository.ReceivedCancellationTokens);

        Assert.Equal(
            new[]
            {
            "GetForUpdate",
            "GetLeaveType",
            "HasOverlap",
            "GetApprovedUsedDaysForYear:2026",
            "GetApprovedUsedDaysForYear:2027"
            },
            callSequence);

        Assert.Equal(
            originalLeaveTypeId,
            leaveRequest.LeaveTypeId);

        Assert.Equal(
            originalStartDate,
            leaveRequest.StartDate);

        Assert.Equal(
            originalEndDate,
            leaveRequest.EndDate);

        Assert.Equal(
            originalRequestedDays,
            leaveRequest.RequestedDays);

        Assert.Equal(
            "Original reason",
            leaveRequest.Reason);

        Assert.Equal(
            originalUpdatedAtUtc,
            leaveRequest.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesAndSavesThenReturnsNullWhenReloadReturnsNull()
    {
        var leaveRequest =
            CreateLeaveRequest();

        var leaveType =
            CreateLeaveType(
                defaultAnnualAllowanceDays: 20);

        var requestedStartDate =
            new DateOnly(
                2026,
                7,
                10);

        var requestedEndDate =
            new DateOnly(
                2026,
                7,
                12);

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    leaveRequest,
                LeaveTypeResult =
                    leaveType,
                HasOverlapResult =
                    false,
                AllowSaveChanges =
                    true
            };

        writeRepository.ApprovedUsedDaysByYear[2026] =
            5;

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence)
            {
                GetByIdResult =
                    null
            };

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                leaveType.Id,
                requestedStartDate,
                requestedEndDate,
                "  Updated reason  ");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var beforeUpdateUtc =
            DateTime.UtcNow;

        var result =
            await handler.Handle(
                command,
                cancellationToken);

        var afterUpdateUtc =
            DateTime.UtcNow;

        Assert.Null(
            result);

        Assert.Equal(
            leaveType.Id,
            leaveRequest.LeaveTypeId);

        Assert.Equal(
            requestedStartDate,
            leaveRequest.StartDate);

        Assert.Equal(
            requestedEndDate,
            leaveRequest.EndDate);

        Assert.Equal(
            3,
            leaveRequest.RequestedDays);

        Assert.Equal(
            "Updated reason",
            leaveRequest.Reason);

        Assert.True(
            leaveRequest.UpdatedAtUtc.HasValue);

        Assert.InRange(
            leaveRequest.UpdatedAtUtc.Value,
            beforeUpdateUtc,
            afterUpdateUtc);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);

        Assert.Equal(
            leaveRequest.Id,
            readRepository.RequestedId);

        var approvedUsedDaysRequest =
            Assert.Single(
                writeRepository.ApprovedUsedDaysRequests);

        Assert.Equal(
            leaveRequest.EmployeeId,
            approvedUsedDaysRequest.EmployeeId);

        Assert.Equal(
            leaveType.Id,
            approvedUsedDaysRequest.LeaveTypeId);

        Assert.Equal(
            2026,
            approvedUsedDaysRequest.Year);

        Assert.Equal(
            leaveRequest.Id,
            approvedUsedDaysRequest.ExcludedLeaveRequestId);

        Assert.Equal(
            new[]
            {
            cancellationToken,
            cancellationToken,
            cancellationToken,
            cancellationToken,
            cancellationToken
            },
            writeRepository.ReceivedCancellationTokens);

        Assert.Equal(
            cancellationToken,
            Assert.Single(
                readRepository.ReceivedCancellationTokens));

        Assert.Equal(
            new[]
            {
            "GetForUpdate",
            "GetLeaveType",
            "HasOverlap",
            "GetApprovedUsedDaysForYear:2026",
            "SaveChanges",
            "GetById"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_ZeroAllowance_SkipsBalanceRestrictionAfterUsageLookup()
    {
        var leaveRequest =
            CreateLeaveRequest();

        var leaveType =
            CreateLeaveType(
                defaultAnnualAllowanceDays: 0);

        var requestedStartDate =
            new DateOnly(
                2026,
                8,
                10);

        var requestedEndDate =
            new DateOnly(
                2026,
                8,
                12);

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    leaveRequest,
                LeaveTypeResult =
                    leaveType,
                HasOverlapResult =
                    false,
                AllowSaveChanges =
                    true
            };

        writeRepository.ApprovedUsedDaysByYear[2026] =
            999;

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence)
            {
                GetByIdResult =
                    null
            };

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                leaveType.Id,
                requestedStartDate,
                requestedEndDate,
                "Unlimited leave update");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var result =
            await handler.Handle(
                command,
                cancellationToken);

        Assert.Null(
            result);

        Assert.Equal(
            1,
            writeRepository.GetApprovedUsedDaysCallCount);

        var approvedUsedDaysRequest =
            Assert.Single(
                writeRepository.ApprovedUsedDaysRequests);

        Assert.Equal(
            leaveRequest.EmployeeId,
            approvedUsedDaysRequest.EmployeeId);

        Assert.Equal(
            leaveType.Id,
            approvedUsedDaysRequest.LeaveTypeId);

        Assert.Equal(
            2026,
            approvedUsedDaysRequest.Year);

        Assert.Equal(
            leaveRequest.Id,
            approvedUsedDaysRequest.ExcludedLeaveRequestId);

        Assert.Equal(
            leaveType.Id,
            leaveRequest.LeaveTypeId);

        Assert.Equal(
            requestedStartDate,
            leaveRequest.StartDate);

        Assert.Equal(
            requestedEndDate,
            leaveRequest.EndDate);

        Assert.Equal(
            3,
            leaveRequest.RequestedDays);

        Assert.Equal(
            "Unlimited leave update",
            leaveRequest.Reason);

        Assert.True(
            leaveRequest.UpdatedAtUtc.HasValue);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);

        Assert.Equal(
            new[]
            {
            cancellationToken,
            cancellationToken,
            cancellationToken,
            cancellationToken,
            cancellationToken
            },
            writeRepository.ReceivedCancellationTokens);

        Assert.Equal(
            cancellationToken,
            Assert.Single(
                readRepository.ReceivedCancellationTokens));

        Assert.Equal(
            new[]
            {
            "GetForUpdate",
            "GetLeaveType",
            "HasOverlap",
            "GetApprovedUsedDaysForYear:2026",
            "SaveChanges",
            "GetById"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsReloadedLeaveRequestDto()
    {
        var leaveRequest =
            CreateLeaveRequest();

        var leaveType =
            CreateLeaveType(
                defaultAnnualAllowanceDays: 20);

        var requestedStartDate =
            new DateOnly(
                2026,
                9,
                10);

        var requestedEndDate =
            new DateOnly(
                2026,
                9,
                12);

        var reloadedDto =
            new LeaveRequestDto(
                leaveRequest.Id,
                leaveRequest.EmployeeId,
                "Test Employee",
                leaveType.Id,
                leaveType.Name,
                requestedStartDate,
                requestedEndDate,
                3,
                LeaveRequestStatus.Pending,
                "Updated reason",
                null,
                null,
                null,
                null,
                leaveRequest.CreatedAtUtc,
                DateTime.UtcNow);

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveRequestResult =
                    leaveRequest,
                LeaveTypeResult =
                    leaveType,
                HasOverlapResult =
                    false,
                AllowSaveChanges =
                    true
            };

        writeRepository.ApprovedUsedDaysByYear[2026] =
            5;

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence)
            {
                GetByIdResult =
                    reloadedDto
            };

        var handler =
            new UpdateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new UpdateLeaveRequestCommand(
                leaveRequest.Id,
                leaveType.Id,
                requestedStartDate,
                requestedEndDate,
                "Updated reason");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var result =
            await handler.Handle(
                command,
                cancellationToken);

        Assert.Same(
            reloadedDto,
            result);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);

        Assert.Equal(
            leaveRequest.Id,
            readRepository.RequestedId);

        Assert.Equal(
            cancellationToken,
            Assert.Single(
                readRepository.ReceivedCancellationTokens));

        Assert.Equal(
            new[]
            {
            "GetForUpdate",
            "GetLeaveType",
            "HasOverlap",
            "GetApprovedUsedDaysForYear:2026",
            "SaveChanges",
            "GetById"
            },
            callSequence);
    }

    private static UpdateLeaveRequestCommand CreateValidCommand(
        Guid leaveRequestId)
    {
        return new UpdateLeaveRequestCommand(
            leaveRequestId,
            Guid.NewGuid(),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12),
            "Updated reason");
    }

    private static LeaveType CreateLeaveType(
        int defaultAnnualAllowanceDays = 20)
    {
        return new LeaveType
        {
            Id =
                Guid.NewGuid(),
            Name =
                "Annual Leave",
            DefaultAnnualAllowanceDays =
                defaultAnnualAllowanceDays,
            IsPaid =
                true
        };
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
                    "Original reason"
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
        private readonly List<string>? callSequence;

        public FakeLeaveRequestWriteRepository(
            List<string>? callSequence = null)
        {
            this.callSequence =
                callSequence;
        }

        public LeaveRequest? LeaveRequestResult
        {
            get;
            init;
        }

        public LeaveType? LeaveTypeResult
        {
            get;
            init;
        }

        public bool HasOverlapResult
        {
            get;
            init;
        }

        public Guid RequestedOverlapEmployeeId
        {
            get;
            private set;
        }

        public DateOnly RequestedOverlapStartDate
        {
            get;
            private set;
        }

        public DateOnly RequestedOverlapEndDate
        {
            get;
            private set;
        }

        public Guid? RequestedOverlapExcludedLeaveRequestId
        {
            get;
            private set;
        }

        public int HasOverlapCallCount
        {
            get;
            private set;
        }

        public Dictionary<int, int> ApprovedUsedDaysByYear
        {
            get;
        } = new();

        public List<(
            Guid EmployeeId,
            Guid LeaveTypeId,
            int Year,
            Guid? ExcludedLeaveRequestId)>
            ApprovedUsedDaysRequests
        {
            get;
        } = new();

        public int GetApprovedUsedDaysCallCount
        {
            get;
            private set;
        }

        public bool AllowSaveChanges
        {
            get;
            init;
        }

        public int SaveChangesCallCount
        {
            get;
            private set;
        }

        public Guid RequestedId
        {
            get;
            private set;
        }

        public Guid RequestedLeaveTypeId
        {
            get;
            private set;
        }

        public int GetForUpdateCallCount
        {
            get;
            private set;
        }

        public int GetLeaveTypeCallCount
        {
            get;
            private set;
        }

        public List<CancellationToken>
            ReceivedCancellationTokens
        {
            get;
        } = new();

        public Task<LeaveRequest?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            GetForUpdateCallCount++;
            RequestedId =
                id;

            ReceivedCancellationTokens.Add(
                cancellationToken);

            callSequence?.Add(
                "GetForUpdate");

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
            GetLeaveTypeCallCount++;

            RequestedLeaveTypeId =
                leaveTypeId;

            ReceivedCancellationTokens.Add(
                cancellationToken);

            callSequence?.Add(
                "GetLeaveType");

            return Task.FromResult(
                LeaveTypeResult);
        }

        public Task<bool> HasOverlappingLeaveRequestAsync(
            Guid employeeId,
            DateOnly startDate,
            DateOnly endDate,
            Guid? excludedLeaveRequestId,
            CancellationToken cancellationToken = default)
        {
            HasOverlapCallCount++;

            RequestedOverlapEmployeeId =
                employeeId;

            RequestedOverlapStartDate =
                startDate;

            RequestedOverlapEndDate =
                endDate;

            RequestedOverlapExcludedLeaveRequestId =
                excludedLeaveRequestId;

            ReceivedCancellationTokens.Add(
                cancellationToken);

            callSequence?.Add(
                "HasOverlap");

            return Task.FromResult(
                HasOverlapResult);
        }

        public Task<int> GetApprovedUsedDaysForYearAsync(
            Guid employeeId,
            Guid leaveTypeId,
            int year,
            Guid? excludedLeaveRequestId,
            CancellationToken cancellationToken = default)
        {
            GetApprovedUsedDaysCallCount++;

            ApprovedUsedDaysRequests.Add(
                (
                    employeeId,
                    leaveTypeId,
                    year,
                    excludedLeaveRequestId
                ));

            ReceivedCancellationTokens.Add(
                cancellationToken);

            callSequence?.Add(
                $"GetApprovedUsedDaysForYear:{year}");

            var usedDays =
                ApprovedUsedDaysByYear.TryGetValue(
                    year,
                    out var configuredUsedDays)
                    ? configuredUsedDays
                    : 0;

            return Task.FromResult(
                usedDays);
        }

        public void Add(
            LeaveRequest leaveRequest)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
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

            callSequence?.Add(
                "SaveChanges");

            return Task.CompletedTask;
        }
    }

    private sealed class FakeLeaveRequestReadRepository
        : ILeaveRequestReadRepository
    {
        private readonly List<string>? callSequence;

        public FakeLeaveRequestReadRepository(
            List<string>? callSequence = null)
        {
            this.callSequence =
                callSequence;
        }

        public LeaveRequestDto? GetByIdResult
        {
            get;
            init;
        }

        public Guid RequestedId
        {
            get;
            private set;
        }

        public int GetByIdCallCount
        {
            get;
            private set;
        }

        public List<CancellationToken>
            ReceivedCancellationTokens
        {
            get;
        } = new();

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

            RequestedId =
                id;

            ReceivedCancellationTokens.Add(
                cancellationToken);

            callSequence?.Add(
                "GetById");

            return Task.FromResult(
                GetByIdResult);
        }
    }

    private sealed class FailFastLeaveRequestReadRepository
        : ILeaveRequestReadRepository
    {
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
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }
    }
}
