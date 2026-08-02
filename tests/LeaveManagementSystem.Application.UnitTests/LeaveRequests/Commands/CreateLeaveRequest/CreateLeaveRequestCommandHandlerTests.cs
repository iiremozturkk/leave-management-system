using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Commands.CreateLeaveRequest;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.LeaveRequests.Commands.CreateLeaveRequest;

public sealed class CreateLeaveRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_NormalizesPersistsAndReturnsLeaveRequest()
    {
        var employeeId = Guid.NewGuid();
        var leaveType = CreateLeaveType(
            defaultAnnualAllowanceDays: 20);

        var startDate = new DateOnly(
            2026,
            6,
            10);

        var endDate = new DateOnly(
            2026,
            6,
            12);

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveTypeResult = leaveType
            };

        LeaveRequestDto? expectedResult = null;

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence)
            {
                ResultFactory = id =>
                {
                    expectedResult =
                        CreateLeaveRequestDto(
                            id,
                            employeeId,
                            leaveType,
                            startDate,
                            endDate,
                            "Family trip");

                    return expectedResult;
                }
            };

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new CreateLeaveRequestCommand(
                employeeId,
                leaveType.Id,
                startDate,
                endDate,
                "  Family trip  ");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var result =
            await handler.Handle(
                command,
                cancellationToken);

        var addedLeaveRequest =
            Assert.IsType<LeaveRequest>(
                writeRepository.AddedLeaveRequest);

        Assert.Equal(
            employeeId,
            addedLeaveRequest.EmployeeId);

        Assert.Equal(
            leaveType.Id,
            addedLeaveRequest.LeaveTypeId);

        Assert.Equal(
            startDate,
            addedLeaveRequest.StartDate);

        Assert.Equal(
            endDate,
            addedLeaveRequest.EndDate);

        Assert.Equal(
            3,
            addedLeaveRequest.RequestedDays);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            addedLeaveRequest.Status);

        Assert.Equal(
            "Family trip",
            addedLeaveRequest.Reason);

        Assert.Equal(
            employeeId,
            writeRepository.RequestedActiveEmployeeId);

        Assert.Equal(
            leaveType.Id,
            writeRepository.RequestedLeaveTypeId);

        Assert.Equal(
            employeeId,
            writeRepository.RequestedOverlapEmployeeId);

        Assert.Equal(
            startDate,
            writeRepository.RequestedOverlapStartDate);

        Assert.Equal(
            endDate,
            writeRepository.RequestedOverlapEndDate);

        Assert.Null(
            writeRepository.RequestedOverlapExcludedLeaveRequestId);

        var balanceRequest =
            Assert.Single(
                writeRepository.ApprovedUsedDaysRequests);

        Assert.Equal(
            employeeId,
            balanceRequest.EmployeeId);

        Assert.Equal(
            leaveType.Id,
            balanceRequest.LeaveTypeId);

        Assert.Equal(
            2026,
            balanceRequest.Year);

        Assert.Null(
            balanceRequest.ExcludedLeaveRequestId);

        Assert.Equal(
            1,
            writeRepository.ActiveEmployeeExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.GetLeaveTypeCallCount);

        Assert.Equal(
            1,
            writeRepository.HasOverlapCallCount);

        Assert.Equal(
            1,
            writeRepository.GetApprovedUsedDaysCallCount);

        Assert.Equal(
            1,
            writeRepository.AddCallCount);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);

        Assert.Equal(
            addedLeaveRequest.Id,
            readRepository.RequestedId);

        Assert.NotNull(expectedResult);
        Assert.Same(
            expectedResult,
            result);

        Assert.Equal(
            5,
            writeRepository.ReceivedCancellationTokens.Count);

        Assert.All(
            writeRepository.ReceivedCancellationTokens,
            receivedCancellationToken =>
                Assert.Equal(
                    cancellationToken,
                    receivedCancellationToken));

        Assert.Single(
            readRepository.ReceivedCancellationTokens);

        Assert.All(
            readRepository.ReceivedCancellationTokens,
            receivedCancellationToken =>
                Assert.Equal(
                    cancellationToken,
                    receivedCancellationToken));

        Assert.Equal(
            new[]
            {
                "ActiveEmployeeExists",
                "GetLeaveType",
                "HasOverlap",
                "GetApprovedUsedDaysForYear:2026",
                "Add",
                "SaveChanges",
                "GetById"
            },
            callSequence);
    }

    [Fact]
    public async Task Handle_EmptyReason_ThrowsBeforeRepositoryCalls()
    {
        var leaveType = CreateLeaveType();

        var writeRepository =
            new FakeLeaveRequestWriteRepository
            {
                LeaveTypeResult = leaveType
            };

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new CreateLeaveRequestCommand(
                Guid.NewGuid(),
                leaveType.Id,
                new DateOnly(2026, 6, 10),
                new DateOnly(2026, 6, 12),
                "   ");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Reason cannot be empty.",
            exception.Message);

        AssertNoRepositoryCalls(
            writeRepository,
            readRepository);
    }

    [Fact]
    public async Task Handle_ReasonExceedsMaximumLength_ThrowsBeforeRepositoryCalls()
    {
        var leaveType =
            CreateLeaveType();

        var writeRepository =
            new FakeLeaveRequestWriteRepository
            {
                LeaveTypeResult = leaveType
            };

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new CreateLeaveRequestCommand(
                Guid.NewGuid(),
                leaveType.Id,
                new DateOnly(2026, 6, 10),
                new DateOnly(2026, 6, 12),
                new string('a', 501));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Reason cannot exceed 500 characters.",
            exception.Message);

        AssertNoRepositoryCalls(
            writeRepository,
            readRepository);
    }

    [Fact]
    public async Task Handle_EndDateBeforeStartDate_ThrowsBeforeRepositoryCalls()
    {
        var leaveType = CreateLeaveType();

        var writeRepository =
            new FakeLeaveRequestWriteRepository
            {
                LeaveTypeResult = leaveType
            };

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new CreateLeaveRequestCommand(
                Guid.NewGuid(),
                leaveType.Id,
                new DateOnly(2026, 6, 12),
                new DateOnly(2026, 6, 10),
                "Family trip");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "End date cannot be earlier than start date.",
            exception.Message);

        AssertNoRepositoryCalls(
            writeRepository,
            readRepository);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public async Task Handle_UnsupportedYear_ThrowsBeforeRepositoryCalls(
        int year)
    {
        var leaveType =
            CreateLeaveType();

        var writeRepository =
            new FakeLeaveRequestWriteRepository
            {
                LeaveTypeResult = leaveType
            };

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new CreateLeaveRequestCommand(
                Guid.NewGuid(),
                leaveType.Id,
                new DateOnly(year, 12, 30),
                new DateOnly(year, 12, 31),
                "Family trip");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Year must be between 2000 and 2100.",
            exception.Message);

        AssertNoRepositoryCalls(
            writeRepository,
            readRepository);
    }

    [Fact]
    public async Task Handle_EmptyEmployeeId_ThrowsWithoutRepositoryCall()
    {
        var leaveType = CreateLeaveType();

        var writeRepository =
            new FakeLeaveRequestWriteRepository
            {
                LeaveTypeResult = leaveType
            };

        var readRepository =
            new FakeLeaveRequestReadRepository();

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
                Guid.Empty,
                leaveType);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Employee id cannot be empty.",
            exception.Message);

        AssertNoRepositoryCalls(
            writeRepository,
            readRepository);
    }

    [Fact]
    public async Task Handle_EmployeeDoesNotExistOrIsInactive_ThrowsAndStopsProcessing()
    {
        var employeeId = Guid.NewGuid();
        var leaveType = CreateLeaveType();

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                ActiveEmployeeExistsResult = false,
                LeaveTypeResult = leaveType
            };

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence);

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
                employeeId,
                leaveType);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Employee does not exist or is not active.",
            exception.Message);

        Assert.Equal(
            new[]
            {
                "ActiveEmployeeExists"
            },
            callSequence);

        Assert.Equal(
            1,
            writeRepository.ActiveEmployeeExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.GetLeaveTypeCallCount);

        AssertDidNotPersist(
            writeRepository,
            readRepository);
    }

    [Fact]
    public async Task Handle_EmptyLeaveTypeId_ThrowsAfterEmployeeCheck()
    {
        var employeeId = Guid.NewGuid();
        var leaveType = CreateLeaveType();

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveTypeResult = leaveType
            };

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence);

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
                employeeId,
                leaveType,
                leaveTypeId: Guid.Empty);

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
                "ActiveEmployeeExists"
            },
            callSequence);

        Assert.Equal(
            1,
            writeRepository.ActiveEmployeeExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.GetLeaveTypeCallCount);

        AssertDidNotPersist(
            writeRepository,
            readRepository);
    }

    [Fact]
    public async Task Handle_LeaveTypeDoesNotExist_ThrowsAndStopsProcessing()
    {
        var employeeId = Guid.NewGuid();
        var leaveType = CreateLeaveType();

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveTypeResult = null
            };

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence);

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
                employeeId,
                leaveType);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Leave type does not exist.",
            exception.Message);

        Assert.Equal(
            new[]
            {
                "ActiveEmployeeExists",
                "GetLeaveType"
            },
            callSequence);

        Assert.Equal(
            0,
            writeRepository.HasOverlapCallCount);

        AssertDidNotPersist(
            writeRepository,
            readRepository);
    }

    [Fact]
    public async Task Handle_OverlappingLeaveRequest_ThrowsBeforeBalanceCheck()
    {
        var employeeId = Guid.NewGuid();
        var leaveType = CreateLeaveType();

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveTypeResult = leaveType,
                HasOverlapResult = true
            };

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence);

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
                employeeId,
                leaveType);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Employee already has a leave request in the selected date range.",
            exception.Message);

        Assert.Equal(
            new[]
            {
                "ActiveEmployeeExists",
                "GetLeaveType",
                "HasOverlap"
            },
            callSequence);

        Assert.Equal(
            0,
            writeRepository.GetApprovedUsedDaysCallCount);

        AssertDidNotPersist(
            writeRepository,
            readRepository);
    }

    [Fact]
    public async Task Handle_InsufficientBalance_ThrowsAndDoesNotPersist()
    {
        var employeeId = Guid.NewGuid();

        var leaveType =
            CreateLeaveType(
                defaultAnnualAllowanceDays: 20);

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveTypeResult = leaveType
            };

        writeRepository.ApprovedUsedDaysByYear[2026] =
            19;

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence);

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new CreateLeaveRequestCommand(
                employeeId,
                leaveType.Id,
                new DateOnly(2026, 6, 10),
                new DateOnly(2026, 6, 11),
                "Family trip");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Requested leave days exceed the remaining leave balance.",
            exception.Message);

        Assert.Equal(
            new[]
            {
                "ActiveEmployeeExists",
                "GetLeaveType",
                "HasOverlap",
                "GetApprovedUsedDaysForYear:2026"
            },
            callSequence);

        AssertDidNotPersist(
            writeRepository,
            readRepository);
    }

    [Fact]
    public async Task Handle_ZeroAllowance_DoesNotApplyBalanceRestriction()
    {
        var employeeId = Guid.NewGuid();

        var leaveType =
            CreateLeaveType(
                defaultAnnualAllowanceDays: 0);

        var writeRepository =
            new FakeLeaveRequestWriteRepository
            {
                LeaveTypeResult = leaveType
            };

        writeRepository.ApprovedUsedDaysByYear[2026] =
            100;

        var readRepository =
            new FakeLeaveRequestReadRepository
            {
                ResultFactory = id =>
                    CreateLeaveRequestDto(
                        id,
                        employeeId,
                        leaveType,
                        new DateOnly(2026, 6, 10),
                        new DateOnly(2026, 6, 12),
                        "Family trip")
            };

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
                employeeId,
                leaveType);

        var result =
            await handler.Handle(
                command,
                CancellationToken.None);

        Assert.NotNull(result);

        Assert.Equal(
            1,
            writeRepository.GetApprovedUsedDaysCallCount);

        Assert.Equal(
            1,
            writeRepository.AddCallCount);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_CrossYearRequest_ChecksEachYearAndPersists()
    {
        var employeeId = Guid.NewGuid();

        var leaveType =
            CreateLeaveType(
                defaultAnnualAllowanceDays: 20);

        var startDate =
            new DateOnly(
                2026,
                12,
                31);

        var endDate =
            new DateOnly(
                2027,
                1,
                2);

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveTypeResult = leaveType
            };

        writeRepository.ApprovedUsedDaysByYear[2026] =
            19;

        writeRepository.ApprovedUsedDaysByYear[2027] =
            18;

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence)
            {
                ResultFactory = id =>
                    CreateLeaveRequestDto(
                        id,
                        employeeId,
                        leaveType,
                        startDate,
                        endDate,
                        "New year trip")
            };

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new CreateLeaveRequestCommand(
                employeeId,
                leaveType.Id,
                startDate,
                endDate,
                "New year trip");

        var result =
            await handler.Handle(
                command,
                CancellationToken.None);

        Assert.NotNull(result);

        Assert.Equal(
            new[]
            {
                2026,
                2027
            },
            writeRepository.ApprovedUsedDaysRequests
                .Select(request => request.Year));

        Assert.All(
            writeRepository.ApprovedUsedDaysRequests,
            request =>
                Assert.Null(
                    request.ExcludedLeaveRequestId));

        Assert.Equal(
            new[]
            {
                "ActiveEmployeeExists",
                "GetLeaveType",
                "HasOverlap",
                "GetApprovedUsedDaysForYear:2026",
                "GetApprovedUsedDaysForYear:2027",
                "Add",
                "SaveChanges",
                "GetById"
            },
            callSequence);

        var addedLeaveRequest =
            Assert.IsType<LeaveRequest>(
                writeRepository.AddedLeaveRequest);

        Assert.Equal(
            3,
            addedLeaveRequest.RequestedDays);
    }

    [Fact]
    public async Task Handle_CrossYearRequest_WhenSecondYearHasInsufficientBalance_ThrowsAndDoesNotPersist()
    {
        var employeeId =
            Guid.NewGuid();

        var leaveType =
            CreateLeaveType(
                defaultAnnualAllowanceDays: 20);

        var startDate =
            new DateOnly(
                2026,
                12,
                31);

        var endDate =
            new DateOnly(
                2027,
                1,
                2);

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveTypeResult = leaveType
            };

        writeRepository.ApprovedUsedDaysByYear[2026] =
            19;

        writeRepository.ApprovedUsedDaysByYear[2027] =
            19;

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence);

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            new CreateLeaveRequestCommand(
                employeeId,
                leaveType.Id,
                startDate,
                endDate,
                "New year trip");

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Requested leave days exceed the remaining leave balance.",
            exception.Message);

        Assert.Equal(
            new[]
            {
                "ActiveEmployeeExists",
                "GetLeaveType",
                "HasOverlap",
                "GetApprovedUsedDaysForYear:2026",
                "GetApprovedUsedDaysForYear:2027"
            },
            callSequence);

        AssertDidNotPersist(
            writeRepository,
            readRepository);
    }

    [Fact]
    public async Task Handle_CreatedLeaveRequestCannotBeReloaded_ThrowsAfterSaving()
    {
        var employeeId = Guid.NewGuid();
        var leaveType = CreateLeaveType();

        var callSequence =
            new List<string>();

        var writeRepository =
            new FakeLeaveRequestWriteRepository(
                callSequence)
            {
                LeaveTypeResult = leaveType
            };

        var readRepository =
            new FakeLeaveRequestReadRepository(
                callSequence);

        var handler =
            new CreateLeaveRequestCommandHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
                employeeId,
                leaveType);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Leave request was created but could not be loaded.",
            exception.Message);

        Assert.Equal(
            1,
            writeRepository.AddCallCount);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);

        Assert.Equal(
            new[]
            {
                "ActiveEmployeeExists",
                "GetLeaveType",
                "HasOverlap",
                "GetApprovedUsedDaysForYear:2026",
                "Add",
                "SaveChanges",
                "GetById"
            },
            callSequence);
    }

    private static CreateLeaveRequestCommand CreateValidCommand(
        Guid employeeId,
        LeaveType leaveType,
        Guid? leaveTypeId = null)
    {
        return new CreateLeaveRequestCommand(
            employeeId,
            leaveTypeId ?? leaveType.Id,
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 12),
            "Family trip");
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

    private static LeaveRequestDto CreateLeaveRequestDto(
        Guid id,
        Guid employeeId,
        LeaveType leaveType,
        DateOnly startDate,
        DateOnly endDate,
        string reason)
    {
        return new LeaveRequestDto(
            id,
            employeeId,
            "Irem Ozturk",
            leaveType.Id,
            leaveType.Name,
            startDate,
            endDate,
            endDate.DayNumber - startDate.DayNumber + 1,
            LeaveRequestStatus.Pending,
            reason,
            null,
            null,
            null,
            null,
            DateTime.UtcNow,
            null);
    }

    private static void AssertNoRepositoryCalls(
        FakeLeaveRequestWriteRepository writeRepository,
        FakeLeaveRequestReadRepository readRepository)
    {
        Assert.Equal(
            0,
            writeRepository.ActiveEmployeeExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.GetLeaveTypeCallCount);

        Assert.Equal(
            0,
            writeRepository.HasOverlapCallCount);

        Assert.Equal(
            0,
            writeRepository.GetApprovedUsedDaysCallCount);

        AssertDidNotPersist(
            writeRepository,
            readRepository);
    }

    private static void AssertDidNotPersist(
        FakeLeaveRequestWriteRepository writeRepository,
        FakeLeaveRequestReadRepository readRepository)
    {
        Assert.Equal(
            0,
            writeRepository.AddCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    private sealed class FakeLeaveRequestWriteRepository
        : ILeaveRequestWriteRepository
    {
        private readonly List<string>? callSequence;

        public FakeLeaveRequestWriteRepository(
            List<string>? callSequence = null)
        {
            this.callSequence = callSequence;
        }

        public bool ActiveEmployeeExistsResult
        {
            get;
            init;
        } = true;

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

        public Dictionary<int, int> ApprovedUsedDaysByYear
        {
            get;
        } = new();

        public LeaveRequest? AddedLeaveRequest
        {
            get;
            private set;
        }

        public Guid RequestedActiveEmployeeId
        {
            get;
            private set;
        }

        public Guid RequestedLeaveTypeId
        {
            get;
            private set;
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

        public List<(
            Guid EmployeeId,
            Guid LeaveTypeId,
            int Year,
            Guid? ExcludedLeaveRequestId)>
            ApprovedUsedDaysRequests
        {
            get;
        } = new();

        public List<CancellationToken>
            ReceivedCancellationTokens
        {
            get;
        } = new();

        public int ActiveEmployeeExistsCallCount
        {
            get;
            private set;
        }

        public int GetLeaveTypeCallCount
        {
            get;
            private set;
        }

        public int HasOverlapCallCount
        {
            get;
            private set;
        }

        public int GetApprovedUsedDaysCallCount
        {
            get;
            private set;
        }

        public int AddCallCount
        {
            get;
            private set;
        }

        public int SaveChangesCallCount
        {
            get;
            private set;
        }

        public Task<bool> ActiveEmployeeExistsAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            ActiveEmployeeExistsCallCount++;
            RequestedActiveEmployeeId = employeeId;

            ReceivedCancellationTokens.Add(
                cancellationToken);

            callSequence?.Add(
                "ActiveEmployeeExists");

            return Task.FromResult(
                ActiveEmployeeExistsResult);
        }

        public Task<LeaveType?> GetLeaveTypeAsync(
            Guid leaveTypeId,
            CancellationToken cancellationToken = default)
        {
            GetLeaveTypeCallCount++;
            RequestedLeaveTypeId = leaveTypeId;

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
            AddCallCount++;
            AddedLeaveRequest = leaveRequest;

            callSequence?.Add(
                "Add");
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
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
            this.callSequence = callSequence;
        }

        public Func<Guid, LeaveRequestDto?>? ResultFactory
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
            RequestedId = id;

            ReceivedCancellationTokens.Add(
                cancellationToken);

            callSequence?.Add(
                "GetById");

            var result =
                ResultFactory?.Invoke(
                    id);

            return Task.FromResult(
                result);
        }
    }
}
