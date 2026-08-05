using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.Employees.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Commands.RejectLeaveRequest;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.LeaveRequests.Commands.RejectLeaveRequest;

public sealed class RejectLeaveRequestCommandHandlerTests
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

        var handler = new RejectLeaveRequestCommandHandler(
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

        var handler = new RejectLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new RejectLeaveRequestCommand(
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

        var handler = new RejectLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new RejectLeaveRequestCommand(
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

        var handler = new RejectLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        var result = await handler.Handle(
            new RejectLeaveRequestCommand(
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
        var originalState = CaptureReviewState(
            leaveRequest);
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

        var handler = new RejectLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        var result = await handler.Handle(
            new RejectLeaveRequestCommand(
                leaveRequest.Id,
                null),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(
            new[] { leaveRequest.EmployeeId },
            employeeReadRepository.RequestedIds);
        AssertReviewStateUnchanged(
            leaveRequest,
            originalState);
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
        var originalState = CaptureReviewState(
            leaveRequest);
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

        var handler = new RejectLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        var result = await handler.Handle(
            new RejectLeaveRequestCommand(
                leaveRequest.Id,
                null),
            CancellationToken.None);

        Assert.Null(result);
        AssertReviewStateUnchanged(
            leaveRequest,
            originalState);
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
    public async Task Handle_DateRangeOutsideSupportedYears_StillRejectsRequest()
    {
        var reviewerId = Guid.NewGuid();

        var leaveRequest = CreateLeaveRequest(
            startDate: new DateOnly(1999, 12, 31),
            endDate: new DateOnly(2101, 1, 1));

        var callSequence = new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(callSequence)
            {
                LeaveRequestResult = leaveRequest,
                AllowSaveChanges = true
            };

        var employeeReadRepository =
            CreateValidEmployeeReadRepository(
                callSequence,
                leaveRequest.EmployeeId,
                reviewerId);

        var expectedDto =
            CreateLeaveRequestDto(
                leaveRequest,
                reviewerId,
                "Rejected");

        var leaveRequestReadRepository =
            new FakeLeaveRequestReadRepository(callSequence)
            {
                AllowGetById = true,
                Result = expectedDto
            };

        var handler = new RejectLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var result = await handler.Handle(
            new RejectLeaveRequestCommand(
                leaveRequest.Id,
                "Rejected"),
            CancellationToken.None);

        Assert.Same(
            expectedDto,
            result);

        Assert.Equal(
            LeaveRequestStatus.Rejected,
            leaveRequest.Status);

        Assert.Equal(
            reviewerId,
            leaveRequest.ReviewedByEmployeeId);

        Assert.Equal(
            "Rejected",
            leaveRequest.ManagerComment);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            leaveRequestReadRepository.GetByIdCallCount);

        Assert.Equal(
            new[]
            {
            "GetCurrentUserAccess",
            "GetForModification",
            $"GetEmployeeById:{leaveRequest.EmployeeId}",
            "SaveChanges",
            "ReloadLeaveRequest"
            },
            callSequence);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Approved)]
    [InlineData(LeaveRequestStatus.Rejected)]
    public async Task Handle_NonPendingRequestWithValidPrerequisites_ThrowsDomainReviewError(
        LeaveRequestStatus status)
    {
        var reviewerId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest(status);
        var originalState = CaptureReviewState(leaveRequest);
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
                reviewerId);

        var leaveRequestReadRepository =
            new FakeLeaveRequestReadRepository(callSequence);

        var handler = new RejectLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    new RejectLeaveRequestCommand(
                        leaveRequest.Id,
                        "Rejected"),
                    CancellationToken.None));

        Assert.Equal(
            "Only pending leave requests can be reviewed.",
            exception.Message);

        AssertReviewStateUnchanged(
            leaveRequest,
            originalState);

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
            $"GetEmployeeById:{leaveRequest.EmployeeId}",
            },
            callSequence);
    }

    [Theory]
    [MemberData(nameof(ValidCommentCases))]
    public async Task Handle_ValidRequest_RejectsAndReturnsReloadedDto(
    string? managerComment,
    string? expectedManagerComment)
    {
        var reviewerId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest();
        var callSequence = new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(callSequence)
            {
                LeaveRequestResult = leaveRequest,
                AllowSaveChanges = true
            };

        var employeeReadRepository =
            CreateValidEmployeeReadRepository(
                callSequence,
                leaveRequest.EmployeeId,
                reviewerId);

        var expectedDto =
            CreateLeaveRequestDto(
                leaveRequest,
                reviewerId,
                expectedManagerComment);

        var leaveRequestReadRepository =
            new FakeLeaveRequestReadRepository(callSequence)
            {
                AllowGetById = true,
                Result = expectedDto
            };

        var currentUserAccessService =
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId);

        var handler = new RejectLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            currentUserAccessService);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var beforeRejectUtc = DateTime.UtcNow;

        var result = await handler.Handle(
            new RejectLeaveRequestCommand(
                leaveRequest.Id,
                managerComment),
            cancellationToken);

        var afterRejectUtc = DateTime.UtcNow;

        Assert.Same(
            expectedDto,
            result);

        Assert.Equal(
            LeaveRequestStatus.Rejected,
            leaveRequest.Status);

        Assert.Equal(
            reviewerId,
            leaveRequest.ReviewedByEmployeeId);

        Assert.Equal(
            expectedManagerComment,
            leaveRequest.ManagerComment);

        Assert.True(
            leaveRequest.ReviewedAtUtc.HasValue);

        Assert.InRange(
            leaveRequest.ReviewedAtUtc.Value,
            beforeRejectUtc,
            afterRejectUtc);

        Assert.Equal(
            leaveRequest.ReviewedAtUtc,
            leaveRequest.UpdatedAtUtc);

        Assert.Equal(
            1,
            writeRepository.GetForModificationCallCount);

        Assert.Equal(
            leaveRequest.Id,
            writeRepository.RequestedLeaveRequestId);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            leaveRequestReadRepository.GetByIdCallCount);

        Assert.Equal(
            leaveRequest.Id,
            Assert.Single(
                leaveRequestReadRepository.RequestedIds));

        Assert.Equal(
            new[]
            {
            leaveRequest.EmployeeId
            },
            employeeReadRepository.RequestedIds);

        Assert.All(
            writeRepository.GetForModificationTokens,
            receivedToken =>
                Assert.Equal(
                    cancellationToken,
                    receivedToken));

        Assert.All(
            writeRepository.SaveChangesTokens,
            receivedToken =>
                Assert.Equal(
                    cancellationToken,
                    receivedToken));

        Assert.Equal(
            cancellationToken,
            Assert.Single(
                currentUserAccessService.ReceivedCancellationTokens));

        Assert.All(
            employeeReadRepository.ReceivedCancellationTokens,
            receivedToken =>
                Assert.Equal(
                    cancellationToken,
                    receivedToken));

        Assert.All(
            leaveRequestReadRepository.ReceivedCancellationTokens,
            receivedToken =>
                Assert.Equal(
                    cancellationToken,
                    receivedToken));

        Assert.Equal(
            new[]
            {
            "GetCurrentUserAccess",
            "GetForModification",
            $"GetEmployeeById:{leaveRequest.EmployeeId}",
            "SaveChanges",
            "ReloadLeaveRequest"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_ReloadReturnsNull_ReturnsNullAfterPersistingRejection()
    {
        var reviewerId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest();
        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest,
            AllowSaveChanges = true
        };

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var leaveRequestReadRepository = new FakeLeaveRequestReadRepository(callSequence)
        {
            AllowGetById = true,
            Result = null
        };

        var handler = new RejectLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var result = await handler.Handle(
            new RejectLeaveRequestCommand(
                leaveRequest.Id,
                null),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(LeaveRequestStatus.Rejected, leaveRequest.Status);
        Assert.Equal(reviewerId, leaveRequest.ReviewedByEmployeeId);
        Assert.Equal(1, writeRepository.SaveChangesCallCount);
        Assert.Equal(1, leaveRequestReadRepository.GetByIdCallCount);

        Assert.Equal(
            leaveRequest.Id,
            Assert.Single(leaveRequestReadRepository.RequestedIds));

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
                "SaveChanges",
                "ReloadLeaveRequest"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_TooLongComment_ThrowsAndLeavesEntityUnchanged()
    {
        var reviewerId = Guid.NewGuid();
        var leaveRequest = CreateLeaveRequest();
        var originalState = CaptureReviewState(leaveRequest);
        var callSequence = new List<string>();

        var writeRepository = new FakeLeaveRequestWriteRepository(callSequence)
        {
            LeaveRequestResult = leaveRequest
        };

        var employeeReadRepository = CreateValidEmployeeReadRepository(
            callSequence,
            leaveRequest.EmployeeId,
            reviewerId);

        var leaveRequestReadRepository = new FakeLeaveRequestReadRepository(callSequence);

        var handler = new RejectLeaveRequestCommandHandler(
            writeRepository,
            leaveRequestReadRepository,
            employeeReadRepository,
            CreateManagerCurrentUserAccessService(
                callSequence,
                reviewerId));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new RejectLeaveRequestCommand(
                    leaveRequest.Id,
                    new string('a', 501)),
                CancellationToken.None));

        Assert.Equal(
            "Manager comment cannot exceed 500 characters.",
            exception.Message);

        AssertReviewStateUnchanged(leaveRequest, originalState);
        Assert.Equal(0, writeRepository.SaveChangesCallCount);
        Assert.Equal(0, leaveRequestReadRepository.GetByIdCallCount);

        Assert.Equal(
            new[]
            {
                "GetCurrentUserAccess",
                "GetForModification",
                $"GetEmployeeById:{leaveRequest.EmployeeId}",
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
            "  Rejected  ",
            "Rejected"
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

    private static ReviewState CaptureReviewState(
        LeaveRequest leaveRequest)
    {
        return new ReviewState(
            leaveRequest.Status,
            leaveRequest.ManagerComment,
            leaveRequest.ReviewedAtUtc,
            leaveRequest.ReviewedByEmployeeId,
            leaveRequest.UpdatedAtUtc);
    }

    private static void AssertReviewStateUnchanged(
        LeaveRequest leaveRequest,
        ReviewState expectedState)
    {
        Assert.Equal(expectedState.Status, leaveRequest.Status);
        Assert.Equal(expectedState.ManagerComment, leaveRequest.ManagerComment);
        Assert.Equal(expectedState.ReviewedAtUtc, leaveRequest.ReviewedAtUtc);
        Assert.Equal(
            expectedState.ReviewedByEmployeeId,
            leaveRequest.ReviewedByEmployeeId);
        Assert.Equal(expectedState.UpdatedAtUtc, leaveRequest.UpdatedAtUtc);
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

    private static LeaveRequest CreateLeaveRequest(
        LeaveRequestStatus status = LeaveRequestStatus.Pending,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            LeaveTypeId = Guid.NewGuid(),
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
            LeaveRequestStatus.Rejected,
            leaveRequest.Reason,
            managerComment,
            DateTime.UtcNow,
            reviewerId,
            "Manager User",
            DateTime.UtcNow,
            DateTime.UtcNow);
    }

    private sealed record ReviewState(
        LeaveRequestStatus Status,
        string? ManagerComment,
        DateTime? ReviewedAtUtc,
        Guid? ReviewedByEmployeeId,
        DateTime? UpdatedAtUtc);

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

    private sealed class FakeLeaveRequestWriteRepository(
        List<string> callSequence)
        : ILeaveRequestWriteRepository
    {
        public LeaveRequest? LeaveRequestResult { get; init; }

        public bool AllowSaveChanges { get; init; }

        public Guid RequestedLeaveRequestId { get; private set; }

        public int GetForModificationCallCount { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public List<CancellationToken> GetForModificationTokens { get; } = new();

        public List<CancellationToken> SaveChangesTokens { get; } = new();

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
        public Func<Guid, EmployeeDto?>? ResultFactory { get; init; }

        public List<Guid> RequestedIds { get; } = new();

        public List<CancellationToken> ReceivedCancellationTokens { get; } = new();

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

        public List<CancellationToken> ReceivedCancellationTokens { get; } = new();

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
