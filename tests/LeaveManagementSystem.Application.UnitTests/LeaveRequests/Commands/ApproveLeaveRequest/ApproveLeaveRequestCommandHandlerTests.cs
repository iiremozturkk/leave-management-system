using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.Employees.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Commands.ApproveLeaveRequest;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.LeaveRequests.Commands.ApproveLeaveRequest;

public sealed class ApproveLeaveRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_NullCommand_ThrowsBeforeDependencyCalls()
    {
        var callSequence = new List<string>();
        var writeRepository =
            new FakeLeaveRequestWriteRepository(callSequence);
        var leaveRequestReadRepository =
            new FakeLeaveRequestReadRepository(callSequence);
        var employeeReadRepository =
            new FakeEmployeeReadRepository(callSequence);
        var currentUserAccessService =
            new FakeCurrentUserAccessService(callSequence);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(
                null!,
                CancellationToken.None));

        Assert.Equal(
            0,
            currentUserAccessService.CallCount);
        Assert.Equal(
            0,
            writeRepository.GetForModificationCallCount);
        Assert.Empty(
            employeeReadRepository.RequestedIds);
        Assert.Equal(
            0,
            leaveRequestReadRepository.GetByIdCallCount);
        Assert.Empty(callSequence);
    }

    [Fact]
    public async Task Handle_CurrentUserAccessMissing_ThrowsForbiddenBeforeRepositoryCalls()
    {
        var callSequence = new List<string>();
        var writeRepository =
            new FakeLeaveRequestWriteRepository(callSequence);
        var leaveRequestReadRepository =
            new FakeLeaveRequestReadRepository(callSequence);
        var employeeReadRepository =
            new FakeEmployeeReadRepository(callSequence);
        var currentUserAccessService =
            new FakeCurrentUserAccessService(callSequence)
            {
                AllowGet = true,
                Result = null
            };

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new ApproveLeaveRequestCommand(
                        Guid.NewGuid(),
                        null),
                    CancellationToken.None));

        Assert.Equal(
            "Only current active managers can review leave requests.",
            exception.Message);
        Assert.Equal(
            1,
            currentUserAccessService.CallCount);
        Assert.Equal(
            0,
            writeRepository.GetForModificationCallCount);
        Assert.Empty(
            employeeReadRepository.RequestedIds);
        Assert.Equal(
            0,
            leaveRequestReadRepository.GetByIdCallCount);
        Assert.Equal(
            new[] { "GetCurrentUserAccess" },
            callSequence);
    }

    [Theory]
    [InlineData(EmployeeRole.Employee)]
    [InlineData(EmployeeRole.HR)]
    public async Task Handle_CurrentUserIsNotManager_ThrowsForbiddenBeforeRepositoryCalls(
        EmployeeRole role)
    {
        var callSequence = new List<string>();
        var writeRepository =
            new FakeLeaveRequestWriteRepository(callSequence);
        var leaveRequestReadRepository =
            new FakeLeaveRequestReadRepository(callSequence);
        var employeeReadRepository =
            new FakeEmployeeReadRepository(callSequence);
        var currentUserAccessService =
            CreateCurrentUserAccessService(
                callSequence,
                Guid.NewGuid(),
                role);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new ApproveLeaveRequestCommand(
                        Guid.NewGuid(),
                        null),
                    CancellationToken.None));

        Assert.Equal(
            "Only current active managers can review leave requests.",
            exception.Message);
        Assert.Equal(
            1,
            currentUserAccessService.CallCount);
        Assert.Equal(
            0,
            writeRepository.GetForModificationCallCount);
        Assert.Empty(
            employeeReadRepository.RequestedIds);
        Assert.Equal(
            0,
            leaveRequestReadRepository.GetByIdCallCount);
        Assert.Equal(
            new[] { "GetCurrentUserAccess" },
            callSequence);
    }

    [Fact]
    public async Task Handle_LeaveRequestDoesNotExist_ReturnsNullAndStopsProcessing()
    {
        var reviewerId = Guid.NewGuid();
        var leaveRequestId = Guid.NewGuid();
        var callSequence = new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(callSequence)
            {
                LeaveRequestResult = null
            };

        var employeeReadRepository =
            new FakeEmployeeReadRepository(callSequence);
        var leaveRequestReadRepository =
            new FakeLeaveRequestReadRepository(callSequence);
        var currentUserAccessService =
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        var result = await handler.Handle(
            new ApproveLeaveRequestCommand(
                leaveRequestId,
                null),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(
            leaveRequestId,
            writeRepository.RequestedLeaveRequestId);
        Assert.Empty(
            employeeReadRepository.RequestedIds);
        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);
        Assert.Equal(
            0,
            leaveRequestReadRepository.GetByIdCallCount);
        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification"
            },
            callSequence);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_RequestOwnerMissingOrInactive_ReturnsNullWithoutMutation(
        bool employeeExists)
    {
        var reviewerId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest();
        var originalStatus = leaveRequest.Status;
        var originalUpdatedAtUtc = leaveRequest.UpdatedAtUtc;
        var callSequence = new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(callSequence)
            {
                LeaveRequestResult = leaveRequest
            };

        var employeeReadRepository =
            new FakeEmployeeReadRepository(callSequence)
            {
                ResultFactory = id =>
                {
                    if (id != leaveRequest.EmployeeId)
                    {
                        throw new InvalidOperationException(
                            "Unexpected repository call.");
                    }

                    return employeeExists
                        ? CreateEmployeeDto(
                            id,
                            isActive: false,
                            EmployeeRole.Employee,
                            reviewerId)
                        : null;
                }
            };

        var leaveRequestReadRepository =
            new FakeLeaveRequestReadRepository(callSequence);
        var currentUserAccessService =
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        var result = await handler.Handle(
            new ApproveLeaveRequestCommand(
                leaveRequest.Id,
                null),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(
            new[] { leaveRequest.EmployeeId },
            employeeReadRepository.RequestedIds);
        Assert.Equal(
            originalStatus,
            leaveRequest.Status);
        Assert.Equal(
            originalUpdatedAtUtc,
            leaveRequest.UpdatedAtUtc);
        Assert.Null(
            leaveRequest.ReviewedByEmployeeId);
        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);
        Assert.Equal(
            0,
            leaveRequestReadRepository.GetByIdCallCount);
        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_CurrentManagerIsNotDirectManager_ReturnsNullWithoutMutation()
    {
        var reviewerId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest();
        var originalStatus = leaveRequest.Status;
        var originalUpdatedAtUtc = leaveRequest.UpdatedAtUtc;
        var callSequence = new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(callSequence)
            {
                LeaveRequestResult = leaveRequest
            };

        var employeeReadRepository =
            CreateValidEmployeeReadRepository(
                callSequence,
                leaveRequest.EmployeeId,
                reviewerId,
                employeeManagerId: Guid.NewGuid());

        var leaveRequestReadRepository =
            new FakeLeaveRequestReadRepository(callSequence);
        var currentUserAccessService =
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        var result = await handler.Handle(
            new ApproveLeaveRequestCommand(
                leaveRequest.Id,
                null),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(
            originalStatus,
            leaveRequest.Status);
        Assert.Equal(
            originalUpdatedAtUtc,
            leaveRequest.UpdatedAtUtc);
        Assert.Null(
            leaveRequest.ReviewedByEmployeeId);
        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);
        Assert.Equal(
            0,
            leaveRequestReadRepository.GetByIdCallCount);
        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_StartYearBelowSupportedRange_ThrowsBeforeBalanceChecks()
    {
        var reviewerId = Guid.NewGuid();

        var leaveRequest = CreateLeaveRequest(
            startDate: new DateOnly(1999, 12, 31),
            endDate: new DateOnly(2000, 1, 1));

        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest
        };

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            new FakeLeaveRequestReadRepository(callSequence),
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new ApproveLeaveRequestCommand(
                    leaveRequest.Id,
                    null),
                CancellationToken.None));

        Assert.Equal(
            "Year must be between 2000 and 2100.",
            exception.Message);

        Assert.Equal(LeaveRequestStatus.Pending, leaveRequest.Status);
        Assert.Empty(writeRepository.RequestedLeaveTypeIds);
        Assert.Empty(writeRepository.ApprovedUsedDaysRequests);
        Assert.Equal(0, writeRepository.SaveChangesCallCount);

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_EndYearAboveSupportedRange_ThrowsBeforeBalanceChecks()
    {
        var reviewerId = Guid.NewGuid();

        var leaveRequest = CreateLeaveRequest(
            startDate: new DateOnly(2100, 12, 31),
            endDate: new DateOnly(2101, 1, 1));

        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest
        };

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            new FakeLeaveRequestReadRepository(callSequence),
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new ApproveLeaveRequestCommand(
                    leaveRequest.Id,
                    null),
                CancellationToken.None));

        Assert.Equal(
            "Year must be between 2000 and 2100.",
            exception.Message);

        Assert.Equal(LeaveRequestStatus.Pending, leaveRequest.Status);
        Assert.Empty(writeRepository.RequestedLeaveTypeIds);
        Assert.Empty(writeRepository.ApprovedUsedDaysRequests);
        Assert.Equal(0, writeRepository.SaveChangesCallCount);

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_LeaveTypeDoesNotExist_ThrowsBeforeUsageAndSave()
    {
        var reviewerId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest();
        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest,
            LeaveTypeResultFactory = _ => null
        };

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            new FakeLeaveRequestReadRepository(callSequence),
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new ApproveLeaveRequestCommand(
                    leaveRequest.Id,
                    null),
                CancellationToken.None));

        Assert.Equal(
            "Leave type does not exist.",
            exception.Message);

        Assert.Equal(
            new[] { leaveRequest.LeaveTypeId },
            writeRepository.RequestedLeaveTypeIds);

        Assert.Empty(writeRepository.ApprovedUsedDaysRequests);
        Assert.Equal(LeaveRequestStatus.Pending, leaveRequest.Status);

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
                "GetLeaveType"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_InsufficientBalance_ThrowsAndLeavesEntityPending()
    {
        var reviewerId = Guid.NewGuid();
        var leaveType = CreateLeaveType(defaultAnnualAllowanceDays: 20);
        var leaveRequest = CreateLeaveRequest(leaveTypeId: leaveType.Id);
        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest,
            LeaveTypeResultFactory = _ => leaveType,
            AllowApprovedUsedDays = true
        };

        writeRepository.ApprovedUsedDaysByYear[2026] = 18;

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            new FakeLeaveRequestReadRepository(callSequence),
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new ApproveLeaveRequestCommand(
                    leaveRequest.Id,
                    null),
                CancellationToken.None));

        Assert.Equal(
            "Requested leave days exceed the remaining leave balance.",
            exception.Message);

        Assert.Equal(LeaveRequestStatus.Pending, leaveRequest.Status);
        Assert.Null(leaveRequest.ReviewedByEmployeeId);
        Assert.Equal(0, writeRepository.SaveChangesCallCount);

        var usageRequest = Assert.Single(
            writeRepository.ApprovedUsedDaysRequests);

        Assert.Equal(leaveRequest.EmployeeId, usageRequest.EmployeeId);
        Assert.Equal(leaveRequest.LeaveTypeId, usageRequest.LeaveTypeId);
        Assert.Equal(2026, usageRequest.Year);
        Assert.Equal(leaveRequest.Id, usageRequest.ExcludedLeaveRequestId);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Approved)]
    [InlineData(LeaveRequestStatus.Rejected)]
    public async Task Handle_NonPendingRequestWithValidPrerequisites_ThrowsDomainReviewError(
        LeaveRequestStatus status)
    {
        var reviewerId = Guid.NewGuid();
        var leaveType = CreateLeaveType();
        var leaveRequest = CreateLeaveRequest(
            leaveTypeId: leaveType.Id,
            status: status);

        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest,
            LeaveTypeResultFactory = _ => leaveType,
            AllowApprovedUsedDays = true
        };

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            new FakeLeaveRequestReadRepository(callSequence),
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new ApproveLeaveRequestCommand(
                    leaveRequest.Id,
                    "Reviewed"),
                CancellationToken.None));

        Assert.Equal(
            "Only pending leave requests can be reviewed.",
            exception.Message);

        Assert.Equal(status, leaveRequest.Status);
        Assert.Equal(0, writeRepository.SaveChangesCallCount);

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
                "GetLeaveType",
                "GetApprovedUsedDaysForYear:2026"
            },
            callSequence);
    }

    [Theory]
    [MemberData(nameof(ValidCommentCases))]
    public async Task Handle_ValidRequest_ApprovesAndReturnsReloadedDto(
        string? managerComment,
        string? expectedManagerComment)
    {
        var reviewerId = Guid.NewGuid();
        var leaveType = CreateLeaveType(defaultAnnualAllowanceDays: 20);
        var leaveRequest = CreateLeaveRequest(leaveTypeId: leaveType.Id);
        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest,
            LeaveTypeResultFactory = _ => leaveType,
            AllowApprovedUsedDays = true,
            AllowSaveChanges = true
        };

        writeRepository.ApprovedUsedDaysByYear[2026] = 17;

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var expectedDto = CreateLeaveRequestDto(
            leaveRequest,
            reviewerId,
            expectedManagerComment);

        var leaveRequestReadRepository = new FakeLeaveRequestReadRepository(
            callSequence)
        {
            AllowGetById = true,
            Result = expectedDto
        };

        var currentUserAccessService =
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var beforeApproveUtc = DateTime.UtcNow;

        var result = await handler.Handle(
            new ApproveLeaveRequestCommand(
                leaveRequest.Id,
                managerComment),
            cancellationToken);

        var afterApproveUtc = DateTime.UtcNow;

        Assert.Same(expectedDto, result);
        Assert.Equal(LeaveRequestStatus.Approved, leaveRequest.Status);
        Assert.Equal(reviewerId, leaveRequest.ReviewedByEmployeeId);
        Assert.Equal(expectedManagerComment, leaveRequest.ManagerComment);
        Assert.True(leaveRequest.ReviewedAtUtc.HasValue);

        Assert.InRange(
            leaveRequest.ReviewedAtUtc.Value,
            beforeApproveUtc,
            afterApproveUtc);

        Assert.Equal(
            leaveRequest.ReviewedAtUtc,
            leaveRequest.UpdatedAtUtc);

        Assert.Equal(1, writeRepository.SaveChangesCallCount);
        Assert.Equal(1, leaveRequestReadRepository.GetByIdCallCount);

        Assert.Equal(
            leaveRequest.Id,
            Assert.Single(leaveRequestReadRepository.RequestedIds));

        Assert.Equal(
            leaveRequest.LeaveTypeId,
            Assert.Single(writeRepository.RequestedLeaveTypeIds));

        var usageRequest = Assert.Single(
            writeRepository.ApprovedUsedDaysRequests);

        Assert.Equal(
            leaveRequest.Id,
            usageRequest.ExcludedLeaveRequestId);

        Assert.All(
            writeRepository.GetForModificationTokens,
            receivedToken => Assert.Equal(
                cancellationToken,
                receivedToken));

        Assert.All(
            writeRepository.GetLeaveTypeTokens,
            receivedToken => Assert.Equal(
                cancellationToken,
                receivedToken));

        Assert.All(
            writeRepository.SaveChangesTokens,
            receivedToken => Assert.Equal(
                cancellationToken,
                receivedToken));

        Assert.Equal(
            cancellationToken,
            usageRequest.CancellationToken);

        Assert.Equal(
            cancellationToken,
            Assert.Single(
                currentUserAccessService.ReceivedCancellationTokens));

        Assert.All(
            employeeReadRepository.ReceivedCancellationTokens,
            receivedToken => Assert.Equal(
                cancellationToken,
                receivedToken));

        Assert.All(
            leaveRequestReadRepository.ReceivedCancellationTokens,
            receivedToken => Assert.Equal(
                cancellationToken,
                receivedToken));

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
                "GetLeaveType",
                "GetApprovedUsedDaysForYear:2026",
                "SaveChanges",
                "ReloadLeaveRequest"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_ReloadReturnsNull_ReturnsNullAfterPersistingApproval()
    {
        var reviewerId = Guid.NewGuid();
        var leaveType = CreateLeaveType();
        var leaveRequest = CreateLeaveRequest(leaveTypeId: leaveType.Id);
        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest,
            LeaveTypeResultFactory = _ => leaveType,
            AllowApprovedUsedDays = true,
            AllowSaveChanges = true
        };

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var leaveRequestReadRepository = new FakeLeaveRequestReadRepository(
            callSequence)
        {
            AllowGetById = true,
            Result = null
        };

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var result = await handler.Handle(
            new ApproveLeaveRequestCommand(
                leaveRequest.Id,
                null),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(LeaveRequestStatus.Approved, leaveRequest.Status);
        Assert.Equal(reviewerId, leaveRequest.ReviewedByEmployeeId);
        Assert.Equal(1, writeRepository.SaveChangesCallCount);
        Assert.Equal(1, leaveRequestReadRepository.GetByIdCallCount);

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
                "GetLeaveType",
                "GetApprovedUsedDaysForYear:2026",
                "SaveChanges",
                "ReloadLeaveRequest"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_ZeroAllowance_QueriesUsageAndApprovesWithoutBalanceRestriction()
    {
        var reviewerId = Guid.NewGuid();
        var leaveType = CreateLeaveType(defaultAnnualAllowanceDays: 0);
        var leaveRequest = CreateLeaveRequest(leaveTypeId: leaveType.Id);
        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest,
            LeaveTypeResultFactory = _ => leaveType,
            AllowApprovedUsedDays = true,
            AllowSaveChanges = true
        };

        writeRepository.ApprovedUsedDaysByYear[2026] = 100;

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var expectedDto = CreateLeaveRequestDto(
            leaveRequest,
            reviewerId,
            null);

        var leaveRequestReadRepository = new FakeLeaveRequestReadRepository(
            callSequence)
        {
            AllowGetById = true,
            Result = expectedDto
        };

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var result = await handler.Handle(
            new ApproveLeaveRequestCommand(
                leaveRequest.Id,
                null),
            CancellationToken.None);

        Assert.Same(expectedDto, result);
        Assert.Equal(LeaveRequestStatus.Approved, leaveRequest.Status);
        Assert.Equal(1, writeRepository.SaveChangesCallCount);
        Assert.Equal(1, leaveRequestReadRepository.GetByIdCallCount);

        var usageRequest = Assert.Single(
            writeRepository.ApprovedUsedDaysRequests);

        Assert.Equal(2026, usageRequest.Year);
        Assert.Equal(leaveRequest.Id, usageRequest.ExcludedLeaveRequestId);

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
                "GetLeaveType",
                "GetApprovedUsedDaysForYear:2026",
                "SaveChanges",
                "ReloadLeaveRequest"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_CrossYearRequest_LoadsLeaveTypeOnceQueriesEachYearAndApproves()
    {
        var reviewerId = Guid.NewGuid();
        var leaveType = CreateLeaveType(defaultAnnualAllowanceDays: 20);

        var leaveRequest = CreateLeaveRequest(
            leaveTypeId: leaveType.Id,
            startDate: new DateOnly(2026, 12, 31),
            endDate: new DateOnly(2027, 1, 2));

        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest,
            LeaveTypeResultFactory = _ => leaveType,
            AllowApprovedUsedDays = true,
            AllowSaveChanges = true
        };

        writeRepository.ApprovedUsedDaysByYear[2026] = 10;
        writeRepository.ApprovedUsedDaysByYear[2027] = 10;

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var expectedDto = CreateLeaveRequestDto(
            leaveRequest,
            reviewerId,
            "Approved");

        var leaveRequestReadRepository = new FakeLeaveRequestReadRepository(
            callSequence)
        {
            AllowGetById = true,
            Result = expectedDto
        };

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var result = await handler.Handle(
            new ApproveLeaveRequestCommand(
                leaveRequest.Id,
                "Approved"),
            CancellationToken.None);

        Assert.Same(expectedDto, result);
        Assert.Equal(LeaveRequestStatus.Approved, leaveRequest.Status);

        Assert.Equal(1, writeRepository.GetLeaveTypeCallCount);

        Assert.Equal(
            new[]
            {
                leaveRequest.LeaveTypeId
            },
            writeRepository.RequestedLeaveTypeIds);

        Assert.Equal(
            new[] { 2026, 2027 },
            writeRepository.ApprovedUsedDaysRequests
                .Select(request => request.Year));

        Assert.All(
            writeRepository.ApprovedUsedDaysRequests,
            request =>
            {
                Assert.Equal(
                    leaveRequest.EmployeeId,
                    request.EmployeeId);

                Assert.Equal(
                    leaveRequest.LeaveTypeId,
                    request.LeaveTypeId);

                Assert.Equal(
                    leaveRequest.Id,
                    request.ExcludedLeaveRequestId);
            });

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
                "GetLeaveType",
                "GetApprovedUsedDaysForYear:2026",
                "GetApprovedUsedDaysForYear:2027",
                "SaveChanges",
                "ReloadLeaveRequest"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_CrossYearSecondYearInsufficient_LoadsLeaveTypeOnceAndStopsWithoutSaving()
    {
        var reviewerId = Guid.NewGuid();
        var leaveType = CreateLeaveType(defaultAnnualAllowanceDays: 20);

        var leaveRequest = CreateLeaveRequest(
            leaveTypeId: leaveType.Id,
            startDate: new DateOnly(2026, 12, 31),
            endDate: new DateOnly(2027, 1, 2));

        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest,
            LeaveTypeResultFactory = _ => leaveType,
            AllowApprovedUsedDays = true
        };

        writeRepository.ApprovedUsedDaysByYear[2026] = 10;
        writeRepository.ApprovedUsedDaysByYear[2027] = 19;

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            new FakeLeaveRequestReadRepository(callSequence),
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new ApproveLeaveRequestCommand(
                    leaveRequest.Id,
                    null),
                CancellationToken.None));

        Assert.Equal(
            "Requested leave days exceed the remaining leave balance.",
            exception.Message);

        Assert.Equal(LeaveRequestStatus.Pending, leaveRequest.Status);
        Assert.Equal(0, writeRepository.SaveChangesCallCount);
        Assert.Equal(1, writeRepository.GetLeaveTypeCallCount);

        Assert.Equal(
            new[]
            {
                leaveRequest.LeaveTypeId
            },
            writeRepository.RequestedLeaveTypeIds);

        Assert.Equal(
            new[] { 2026, 2027 },
            writeRepository.ApprovedUsedDaysRequests
                .Select(request => request.Year));

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
                "GetLeaveType",
                "GetApprovedUsedDaysForYear:2026",
                "GetApprovedUsedDaysForYear:2027"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_TooLongComment_ThrowsAfterBalanceChecksAndLeavesEntityUnchanged()
    {
        var reviewerId = Guid.NewGuid();
        var leaveType = CreateLeaveType();
        var leaveRequest = CreateLeaveRequest(leaveTypeId: leaveType.Id);
        var originalUpdatedAtUtc = leaveRequest.UpdatedAtUtc;
        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest,
            LeaveTypeResultFactory = _ => leaveType,
            AllowApprovedUsedDays = true
        };

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var handler = new ApproveLeaveRequestCommandHandler(
            writeRepository,
            new FakeLeaveRequestReadRepository(callSequence),
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new ApproveLeaveRequestCommand(
                    leaveRequest.Id,
                    new string('a', 501)),
                CancellationToken.None));

        Assert.Equal(
            "Manager comment cannot exceed 500 characters.",
            exception.Message);

        Assert.Equal(LeaveRequestStatus.Pending, leaveRequest.Status);
        Assert.Null(leaveRequest.ManagerComment);
        Assert.Null(leaveRequest.ReviewedAtUtc);
        Assert.Null(leaveRequest.ReviewedByEmployeeId);
        Assert.Equal(originalUpdatedAtUtc, leaveRequest.UpdatedAtUtc);
        Assert.Equal(0, writeRepository.SaveChangesCallCount);

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
                "GetLeaveType",
                "GetApprovedUsedDaysForYear:2026"
            },
            callSequence);
    }

    public static IEnumerable<object?[]> ValidCommentCases()
    {
        yield return new object?[]
        {
            null,
            null
        };

        yield return new object?[]
        {
            "  Approved  ",
            "Approved"
        };

        yield return new object?[]
        {
            "   ",
            null
        };

        var maximumLengthComment = new string('a', 500);

        yield return new object?[]
        {
            maximumLengthComment,
            maximumLengthComment
        };
    }

    private static FakeCurrentUserAccessService CreateManagerCurrentUserAccessService(
        List<string> callSequence,
        Guid reviewerEmployeeId)
    {
        return CreateCurrentUserAccessService(
            callSequence,
            reviewerEmployeeId,
            EmployeeRole.Manager);
    }

    private static FakeCurrentUserAccessService CreateCurrentUserAccessService(
        List<string> callSequence,
        Guid employeeId,
        EmployeeRole role)
    {
        return new FakeCurrentUserAccessService(callSequence)
        {
            AllowGet = true,
            Result = new CurrentUserAccess(
                Guid.NewGuid(),
                employeeId,
                $"{employeeId}@example.com",
                role)
        };
    }

    private static FakeEmployeeReadRepository CreateValidEmployeeReadRepository(
        List<string> callSequence,
        Guid employeeId,
        Guid reviewerId,
        Guid? employeeManagerId = null)
    {
        var managerId =
            employeeManagerId ?? reviewerId;

        return new FakeEmployeeReadRepository(callSequence)
        {
            ResultFactory = id =>
            {
                if (id != employeeId)
                {
                    throw new InvalidOperationException(
                        "Unexpected repository call.");
                }

                return CreateEmployeeDto(
                    id,
                    isActive: true,
                    EmployeeRole.Employee,
                    managerId);
            }
        };
    }

    private static EmployeeDto CreateEmployeeDto(
        Guid id,
        bool isActive,
        EmployeeRole role,
        Guid? managerId)
    {
        return new EmployeeDto(
            id,
            "Irem",
            "Ozturk",
            $"{id}@example.com",
            role,
            isActive,
            Guid.NewGuid(),
            "Engineering",
            managerId,
            managerId.HasValue
                ? "Manager User"
                : null,
            DateTime.UtcNow,
            null);
    }

    private static LeaveType CreateLeaveType(
        int defaultAnnualAllowanceDays = 20)
    {
        return new LeaveType
        {
            Id = Guid.NewGuid(),
            Name = "Annual Leave",
            DefaultAnnualAllowanceDays =
                defaultAnnualAllowanceDays,
            IsPaid = true
        };
    }

    private static LeaveRequest CreateLeaveRequest(
        Guid? leaveTypeId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        LeaveRequestStatus status = LeaveRequestStatus.Pending)
    {
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            LeaveTypeId = leaveTypeId ?? Guid.NewGuid(),
            Reason = "Annual leave"
        };

        leaveRequest.SetDateRange(
            startDate ?? new DateOnly(2026, 6, 10),
            endDate ?? new DateOnly(2026, 6, 12));

        switch (status)
        {
            case LeaveRequestStatus.Pending:
                break;

            case LeaveRequestStatus.Approved:
                leaveRequest.Approve(
                    Guid.NewGuid(),
                    "Previously approved.");
                break;

            case LeaveRequestStatus.Rejected:
                leaveRequest.Reject(
                    Guid.NewGuid(),
                    "Previously rejected.");
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Unsupported leave request status.");
        }

        return leaveRequest;
    }

    private static LeaveRequestDto CreateLeaveRequestDto(
        LeaveRequest leaveRequest,
        Guid reviewerId,
        string? managerComment)
    {
        return new LeaveRequestDto(
            leaveRequest.Id,
            leaveRequest.EmployeeId,
            "Irem Ozturk",
            leaveRequest.LeaveTypeId,
            "Annual Leave",
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.RequestedDays,
            LeaveRequestStatus.Approved,
            leaveRequest.Reason,
            managerComment,
            DateTime.UtcNow,
            reviewerId,
            "Manager User",
            DateTime.UtcNow,
            DateTime.UtcNow);
    }

    private sealed class FakeCurrentUserAccessService(
        List<string> callSequence)
        : ICurrentUserAccessService
    {
        public bool AllowGet { get; init; }

        public CurrentUserAccess? Result { get; init; }

        public int CallCount { get; private set; }

        public List<CancellationToken> ReceivedCancellationTokens
        {
            get;
        } = new();

        public Task<CurrentUserAccess?> GetAsync(
            CancellationToken cancellationToken = default)
        {
            if (!AllowGet)
            {
                throw new InvalidOperationException(
                    "Unexpected current-user access call.");
            }

            CallCount++;
            ReceivedCancellationTokens.Add(
                cancellationToken);
            callSequence.Add(
                "GetCurrentUserAccess");

            return Task.FromResult(Result);
        }
    }

    private sealed record ApprovedUsedDaysRequest(
        Guid EmployeeId,
        Guid LeaveTypeId,
        int Year,
        Guid? ExcludedLeaveRequestId,
        CancellationToken CancellationToken);

    private sealed class FakeLeaveRequestWriteRepository(
        List<string> callSequence)
        : ILeaveRequestWriteRepository
    {
        public LeaveRequest? LeaveRequestResult { get; init; }

        public Func<Guid, LeaveType?>?
            LeaveTypeResultFactory
        {
            get;
            init;
        }

        public bool AllowApprovedUsedDays { get; init; }

        public bool AllowSaveChanges { get; init; }

        public Dictionary<int, int> ApprovedUsedDaysByYear
        {
            get;
        } = new();

        public List<Guid> RequestedLeaveTypeIds
        {
            get;
        } = new();

        public List<ApprovedUsedDaysRequest> ApprovedUsedDaysRequests
        {
            get;
        } = new();

        public List<CancellationToken> GetForModificationTokens
        {
            get;
        } = new();

        public List<CancellationToken> GetLeaveTypeTokens
        {
            get;
        } = new();

        public List<CancellationToken> SaveChangesTokens
        {
            get;
        } = new();

        public Guid RequestedLeaveRequestId { get; private set; }

        public int GetForModificationCallCount { get; private set; }

        public int GetLeaveTypeCallCount { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public Task<LeaveRequest?> GetForModificationAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            GetForModificationCallCount++;
            RequestedLeaveRequestId = id;
            GetForModificationTokens.Add(cancellationToken);
            callSequence.Add("GetForModification");

            return Task.FromResult(LeaveRequestResult);
        }

        public Task<LeaveRequest?> GetForModificationForEmployeeAsync(
            Guid id,
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
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
            if (LeaveTypeResultFactory is null)
            {
                throw new InvalidOperationException(
                    "Unexpected repository call.");
            }

            GetLeaveTypeCallCount++;
            RequestedLeaveTypeIds.Add(leaveTypeId);
            GetLeaveTypeTokens.Add(cancellationToken);
            callSequence.Add("GetLeaveType");

            return Task.FromResult(
                LeaveTypeResultFactory(
                    leaveTypeId));
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
            if (!AllowApprovedUsedDays)
            {
                throw new InvalidOperationException(
                    "Unexpected repository call.");
            }

            ApprovedUsedDaysRequests.Add(
                new ApprovedUsedDaysRequest(
                    employeeId,
                    leaveTypeId,
                    year,
                    excludedLeaveRequestId,
                    cancellationToken));

            callSequence.Add(
                $"GetApprovedUsedDaysForYear:{year}");

            var usedDays = ApprovedUsedDaysByYear.TryGetValue(
                year,
                out var configuredUsedDays)
                ? configuredUsedDays
                : 0;

            return Task.FromResult(usedDays);
        }

        public void Add(LeaveRequest leaveRequest)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public void Remove(LeaveRequest leaveRequest)
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
            SaveChangesTokens.Add(cancellationToken);
            callSequence.Add("SaveChanges");

            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmployeeReadRepository(
        List<string> callSequence)
        : IEmployeeReadRepository
    {
        public Func<Guid, EmployeeDto?>? ResultFactory
        {
            get;
            init;
        }

        public List<Guid> RequestedIds { get; } = new();

        public List<CancellationToken> ReceivedCancellationTokens
        {
            get;
        } = new();

        public Task<IReadOnlyList<EmployeeDto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public Task<EmployeeDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (ResultFactory is null)
            {
                throw new InvalidOperationException(
                    "Unexpected repository call.");
            }

            RequestedIds.Add(id);
            ReceivedCancellationTokens.Add(cancellationToken);
            callSequence.Add($"GetEmployeeById:{id}");

            return Task.FromResult(ResultFactory(id));
        }
    }

    private sealed class FakeLeaveRequestReadRepository(
        List<string> callSequence)
        : ILeaveRequestReadRepository
    {
        public bool AllowGetById { get; init; }

        public LeaveRequestDto? Result { get; init; }

        public List<Guid> RequestedIds { get; } = new();

        public List<CancellationToken> ReceivedCancellationTokens
        {
            get;
        } = new();

        public int GetByIdCallCount { get; private set; }

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
            if (!AllowGetById)
            {
                throw new InvalidOperationException(
                    "Unexpected repository call.");
            }

            GetByIdCallCount++;
            RequestedIds.Add(id);
            ReceivedCancellationTokens.Add(cancellationToken);
            callSequence.Add("ReloadLeaveRequest");

            return Task.FromResult(Result);
        }
    }
}
