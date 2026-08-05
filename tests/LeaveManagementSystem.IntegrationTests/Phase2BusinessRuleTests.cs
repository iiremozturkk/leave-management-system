using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests;

public sealed class Phase2BusinessRuleTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly Guid AnnualLeaveTypeId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private const string Password =
        "Phase2-Claim-Authorization-Test-Password-123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Phase2BusinessRuleTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task OverlappingLeaveRequest_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            await CreateLeaveRequestAsync(
                                  testData,
                "2026-08-10",
                "2026-08-14",
                "First leave request.");

            var overlapRequest = new
            {
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-08-12",
                endDate = "2026-08-13",
                reason = "Overlapping leave request."
            };

            using var response =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Post,
                    "/api/leave-requests",
                    testData.EmployeeAccessToken,
                    overlapRequest);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonOptions);

            Assert.NotNull(problem);
            Assert.Equal(400, problem!.Status);
            Assert.Equal("Invalid leave request.", problem.Title);
            Assert.Contains("selected date range", problem.Detail);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Theory]
    [InlineData("approve")]
    [InlineData("reject")]
    public async Task ReviewLeaveRequest_WithoutToken_ReturnsUnauthorized(
        string reviewAction)
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                                                     testData,
                "2026-08-10",
                "2026-08-14",
                "Anonymous review authorization test.");

            var requestPath =
                $"/api/leave-requests/{leaveRequest.Id}/{reviewAction}";

            using var response =
                await _client.PostAsJsonAsync(
                    requestPath,
                    new
                    {
                        managerComment =
                            "Anonymous users cannot review leave requests."
                    });

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);

            Assert.Contains(
                response.Headers.WwwAuthenticate,
                header =>
                    string.Equals(
                        header.Scheme,
                        "Bearer",
                        StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await CleanupAsync(
                testData.DepartmentId);
        }
    }

    [Fact]
    public async Task NonDirectManagerCannotApproveLeaveRequest_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-08-10",
                "2026-08-14",
                "Approval scope test leave request.");

            using var response =
                await SendAuthorizedReviewRequestAsync(
                    leaveRequest.Id,
                    "approve",
                    testData.OtherManagerAccessToken,
                    "Trying to approve as another manager.");

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                testData.DepartmentId);
        }
    }

    [Theory]
    [InlineData(EmployeeRole.Employee, "approve")]
    [InlineData(EmployeeRole.Employee, "reject")]
    [InlineData(EmployeeRole.HR, "approve")]
    [InlineData(EmployeeRole.HR, "reject")]
    public async Task NonManagerCannotReviewLeaveRequest_ReturnsSafeForbidden(
        EmployeeRole role,
        string reviewAction)
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-08-10",
                "2026-08-14",
                "Role authorization test leave request.");

            var accessToken =
                role switch
                {
                    EmployeeRole.Employee =>
                        testData.EmployeeAccessToken,

                    EmployeeRole.HR =>
                        testData.HrAccessToken,

                    _ =>
                        throw new ArgumentOutOfRangeException(
                            nameof(role),
                            role,
                            "Unsupported non-manager role.")
                };

            var requestPath =
                $"/api/leave-requests/{leaveRequest.Id}/{reviewAction}";

            using var response =
                await SendAuthorizedReviewRequestAsync(
                    leaveRequest.Id,
                    reviewAction,
                    accessToken,
                    "A non-manager must not review leave requests.");

            await AssertForbiddenProblemDetailsAsync(
                response,
                requestPath);
        }
        finally
        {
            await CleanupAsync(
                testData.DepartmentId);
        }
    }

    [Fact]
    public async Task DirectManagerCanApproveLeaveRequest_AndBalanceIsUpdated()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var balanceBefore =
                await GetBalanceAsync(
                    testData,
                    2026);

            Assert.Equal(
                2026,
                balanceBefore.Year);

            Assert.Equal(
                20,
                balanceBefore.EntitledDays);

            Assert.Equal(
                0,
                balanceBefore.UsedDays);

            Assert.Equal(
                20,
                balanceBefore.RemainingDays);

            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-08-10",
                "2026-08-14",
                "Manager approval test leave request.");

            var attackerControlledReviewerId =
                testData.OtherManagerId;

            var approvedLeaveRequest =
                await ApproveLeaveRequestAsync(
                    leaveRequest.Id,
                    testData.ManagerAccessToken,
                    "Approved by direct manager.",
                    attackerControlledReviewerId);

            Assert.Equal(
                LeaveRequestStatus.Approved,
                approvedLeaveRequest.Status);

            Assert.Equal(
                testData.ManagerId,
                approvedLeaveRequest.ReviewedByEmployeeId);

            Assert.NotEqual(
                attackerControlledReviewerId,
                approvedLeaveRequest.ReviewedByEmployeeId);

            Assert.Equal(
                "Approved by direct manager.",
                approvedLeaveRequest.ManagerComment);

            var persistedReviewerEmployeeId =
                await GetReviewedByEmployeeIdAsync(
                    leaveRequest.Id);

            Assert.Equal(
                testData.ManagerId,
                persistedReviewerEmployeeId);

            var balanceAfter =
                await GetBalanceAsync(
                    testData,
                    2026);

            Assert.Equal(
                2026,
                balanceAfter.Year);

            Assert.Equal(
                20,
                balanceAfter.EntitledDays);

            Assert.Equal(
                5,
                balanceAfter.UsedDays);

            Assert.Equal(
                15,
                balanceAfter.RemainingDays);
        }
        finally
        {
            await CleanupAsync(
                testData.DepartmentId);
        }
    }

    [Fact]
    public async Task LeaveRequestExceedingRemainingBalance_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-08-10",
                "2026-08-14",
                "Approved leave request.");

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerAccessToken,
                "Approved by direct manager.");

            var exceedBalanceRequest = new
            {
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-09-01",
                endDate = "2026-09-30",
                reason = "Exceed balance test."
            };

            using var response =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Post,
                    "/api/leave-requests",
                    testData.EmployeeAccessToken,
                    exceedBalanceRequest);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonOptions);

            Assert.NotNull(problem);
            Assert.Equal(400, problem!.Status);
            Assert.Equal("Invalid leave request.", problem.Title);
            Assert.Contains("remaining leave balance", problem.Detail);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Fact]
    public async Task RequestedDaysEqualToRemainingBalance_IsAllowed()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-11-01",
                "2026-11-20",
                "Request exactly the remaining annual balance.");

            Assert.Equal(20, leaveRequest.RequestedDays);

            var approvedLeaveRequest = await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerAccessToken,
                "Approved exact remaining balance.");

            Assert.Equal(LeaveRequestStatus.Approved, approvedLeaveRequest.Status);

            var balanceAfter = await GetBalanceAsync(testData, 2026);

            Assert.Equal(20, balanceAfter.EntitledDays);
            Assert.Equal(20, balanceAfter.UsedDays);
            Assert.Equal(0, balanceAfter.RemainingDays);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Fact]
    public async Task ApprovedLeaveRequestInPreviousYear_DoesNotReduceCurrentYearBalance()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2025-08-10",
                "2025-08-14",
                "Previous year approved leave request.");

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerAccessToken,
                "Approved previous year leave.");

            var previousYearBalance = await GetBalanceAsync(testData, 2025);

            Assert.Equal(2025, previousYearBalance.Year);
            Assert.Equal(20, previousYearBalance.EntitledDays);
            Assert.Equal(5, previousYearBalance.UsedDays);
            Assert.Equal(15, previousYearBalance.RemainingDays);

            var currentYearBalance = await GetBalanceAsync(testData, 2026);

            Assert.Equal(2026, currentYearBalance.Year);
            Assert.Equal(20, currentYearBalance.EntitledDays);
            Assert.Equal(0, currentYearBalance.UsedDays);
            Assert.Equal(20, currentYearBalance.RemainingDays);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Fact]
    public async Task CrossYearApprovedLeaveRequest_ReducesEachYearBalanceCorrectly()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-12-30",
                "2027-01-02",
                "Cross-year leave request.");

            Assert.Equal(4, leaveRequest.RequestedDays);

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerAccessToken,
                "Approved cross-year leave.");

            var balance2026 = await GetBalanceAsync(testData, 2026);

            Assert.Equal(2026, balance2026.Year);
            Assert.Equal(20, balance2026.EntitledDays);
            Assert.Equal(2, balance2026.UsedDays);
            Assert.Equal(18, balance2026.RemainingDays);

            var balance2027 = await GetBalanceAsync(testData, 2027);

            Assert.Equal(2027, balance2027.Year);
            Assert.Equal(20, balance2027.EntitledDays);
            Assert.Equal(2, balance2027.UsedDays);
            Assert.Equal(18, balance2027.RemainingDays);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Fact]
    public async Task CrossYearLeaveRequestExceedingOneYearBalance_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var existingLeaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-01-01",
                "2026-01-19",
                "Existing approved leave request.");

            await ApproveLeaveRequestAsync(
                existingLeaveRequest.Id,
                testData.ManagerAccessToken,
                "Approved existing leave request.");

            var crossYearRequest = new
            {
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-12-30",
                endDate = "2027-01-02",
                reason = "Cross-year leave exceeding one year balance."
            };

            using var response =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Post,
                    "/api/leave-requests",
                    testData.EmployeeAccessToken,
                    crossYearRequest);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonOptions);

            Assert.NotNull(problem);
            Assert.Equal(400, problem!.Status);
            Assert.Equal("Invalid leave request.", problem.Title);
            Assert.Contains("remaining leave balance", problem.Detail);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Fact]
    public async Task LeaveTypeWithZeroAllowance_SkipsBalanceCheck()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();
        var zeroAllowanceLeaveTypeId = await CreateZeroAllowanceLeaveTypeAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-01-01",
                "2026-03-31",
                "Zero allowance leave type should skip balance check.",
                zeroAllowanceLeaveTypeId);

            Assert.Equal(zeroAllowanceLeaveTypeId, leaveRequest.LeaveTypeId);
            Assert.Equal(90, leaveRequest.RequestedDays);
            Assert.Equal(LeaveRequestStatus.Pending, leaveRequest.Status);

            var approvedLeaveRequest = await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerAccessToken,
                "Approved zero allowance leave request.");

            Assert.Equal(LeaveRequestStatus.Approved, approvedLeaveRequest.Status);
            Assert.Equal(testData.ManagerId, approvedLeaveRequest.ReviewedByEmployeeId);
            Assert.Equal("Approved zero allowance leave request.", approvedLeaveRequest.ManagerComment);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
            await CleanupLeaveTypeAsync(zeroAllowanceLeaveTypeId);
        }
    }

    [Fact]
    public async Task ApprovedLeaveRequestCannotBeReviewedAgain_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-10-01",
                "2026-10-03",
                "Approved leave request should not be reviewed again.");

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerAccessToken,
                "Approved by direct manager.");

            using var response =
                await SendAuthorizedReviewRequestAsync(
                    leaveRequest.Id,
                    "reject",
                    testData.ManagerAccessToken,
                    "Trying to reject an already approved request.");

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);

            var problem =
                await response.Content
                    .ReadFromJsonAsync<ProblemDetailsResponse>(
                        JsonOptions);

            Assert.NotNull(
                problem);

            Assert.Equal(
                400,
                problem!.Status);

            Assert.Equal(
                "Invalid leave request.",
                problem.Title);

            Assert.Contains(
                "pending",
                problem.Detail.ToLowerInvariant());
        }
        finally
        {
            await CleanupAsync(
                testData.DepartmentId);
        }
    }

    [Fact]
    public async Task ApprovedLeaveRequestCannotBeUpdated_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-10-01",
                "2026-10-03",
                "Approved leave request should not be updated.");

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerAccessToken,
                "Approved by direct manager.");

            var updateRequest = new
            {
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-10-05",
                endDate = "2026-10-07",
                reason = "Trying to update an approved leave request."
            };

            using var response =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Put,
                    $"/api/leave-requests/{leaveRequest.Id}",
                    testData.EmployeeAccessToken,
                    updateRequest);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonOptions);

            Assert.NotNull(problem);
            Assert.Equal(400, problem!.Status);
            Assert.Equal("Invalid leave request.", problem.Title);
            Assert.Contains("pending", problem.Detail.ToLowerInvariant());
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Fact]
    public async Task ApprovedLeaveRequestCannotBeDeleted_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-10-01",
                "2026-10-03",
                "Approved leave request should not be deleted.");

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerAccessToken,
                "Approved by direct manager.");

            using var response =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Delete,
                    $"/api/leave-requests/{leaveRequest.Id}",
                    testData.EmployeeAccessToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonOptions);

            Assert.NotNull(problem);
            Assert.Equal(400, problem!.Status);
            Assert.Equal("Invalid leave request.", problem.Title);
            Assert.Contains("pending", problem.Detail.ToLowerInvariant());
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Fact]
    public async Task RejectedLeaveRequest_DoesNotBlockNewRequestForSameDateRange()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var rejectedLeaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-10-01",
                "2026-10-03",
                "Rejected leave request should not block same dates.");

            await RejectLeaveRequestAsync(
                rejectedLeaveRequest.Id,
                testData.ManagerAccessToken,
                "Rejected by direct manager.");

            var newLeaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-10-01",
                "2026-10-03",
                "New request for same dates after rejection.");

            Assert.Equal(LeaveRequestStatus.Pending, newLeaveRequest.Status);
            Assert.Equal(3, newLeaveRequest.RequestedDays);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Fact]
    public async Task ApproveNonExistentLeaveRequest_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            using var response =
                await SendAuthorizedReviewRequestAsync(
                    Guid.NewGuid(),
                    "approve",
                    testData.ManagerAccessToken,
                    "Trying to approve a non-existent leave request.");

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                testData.DepartmentId);
        }
    }

    [Fact]
    public async Task FormerDirectManagerCannotRejectLeaveRequest_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-10-10",
                "2026-10-12",
                "Former direct manager rejection test.");

            await ChangeEmployeeManagerAsync(
                testData.EmployeeId,
                testData.OtherManagerId);

            using var response =
                await SendAuthorizedReviewRequestAsync(
                    leaveRequest.Id,
                    "reject",
                    testData.ManagerAccessToken,
                    "Trying to reject after the manager assignment changed.");

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                testData.DepartmentId);
        }
    }

    [Fact]
    public async Task RejectNonExistentLeaveRequest_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            using var response =
                await SendAuthorizedReviewRequestAsync(
                    Guid.NewGuid(),
                    "reject",
                    testData.ManagerAccessToken,
                    "Trying to reject a non-existent leave request.");

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                testData.DepartmentId);
        }
    }

    [Fact]
    public async Task DirectManagerCanRejectLeaveRequest()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData,
                "2026-10-01",
                "2026-10-03",
                "Reject flow test leave request.");

            var rejectedLeaveRequest =
                await RejectLeaveRequestAsync(
                    leaveRequest.Id,
                    testData.ManagerAccessToken,
                    "Rejected by direct manager.");

            Assert.Equal(
                LeaveRequestStatus.Rejected,
                rejectedLeaveRequest.Status);

            Assert.Equal(
                testData.ManagerId,
                rejectedLeaveRequest.ReviewedByEmployeeId);

            Assert.Equal(
                "Rejected by direct manager.",
                rejectedLeaveRequest.ManagerComment);

            var persistedReviewerEmployeeId =
                await GetReviewedByEmployeeIdAsync(
                    leaveRequest.Id);

            Assert.Equal(
                testData.ManagerId,
                persistedReviewerEmployeeId);
        }
        finally
        {
            await CleanupAsync(
                testData.DepartmentId);
        }
    }

    [Fact]
    public async Task GetLeaveRequests_ReturnsRoleScopedCollections()
    {
        await EnsureDatabaseReadyAsync();

        var testData =
            await CreateTeamAsync();

        var managerOwnLeaveRequestId =
            Guid.NewGuid();

        var activeDirectReportLeaveRequestId =
            Guid.NewGuid();

        var inactiveDirectReportId =
            Guid.NewGuid();

        var inactiveDirectReportLeaveRequestId =
            Guid.NewGuid();

        var otherManagersLeaveRequestId =
            Guid.NewGuid();

        try
        {
            var managerOwnLeaveRequest =
                new LeaveRequest
                {
                    Id =
                        managerOwnLeaveRequestId,

                    EmployeeId =
                        testData.ManagerId,

                    LeaveTypeId =
                        AnnualLeaveTypeId,

                    Reason =
                        "Manager own collection scope request.",

                    CreatedAtUtc =
                        DateTime.UtcNow,

                    UpdatedAtUtc =
                        null
                };

            managerOwnLeaveRequest.SetDateRange(
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 9, 2));

            var activeDirectReportLeaveRequest =
                new LeaveRequest
                {
                    Id =
                        activeDirectReportLeaveRequestId,

                    EmployeeId =
                        testData.EmployeeId,

                    LeaveTypeId =
                        AnnualLeaveTypeId,

                    Reason =
                        "Active direct report collection scope request.",

                    CreatedAtUtc =
                        DateTime.UtcNow.AddMinutes(1),

                    UpdatedAtUtc =
                        null
                };

            activeDirectReportLeaveRequest.SetDateRange(
                new DateOnly(2026, 9, 10),
                new DateOnly(2026, 9, 11));

            var inactiveDirectReport =
                new Employee
                {
                    Id =
                        inactiveDirectReportId,

                    FirstName =
                        "Inactive",

                    LastName =
                        "DirectReport",

                    Email =
                        $"inactive.direct.report.{Guid.NewGuid():N}@example.com",

                    DepartmentId =
                        testData.DepartmentId,

                    ManagerId =
                        testData.ManagerId,

                    Role =
                        EmployeeRole.Employee,

                    IsActive =
                        false,

                    CreatedAtUtc =
                        DateTime.UtcNow,

                    UpdatedAtUtc =
                        null
                };

            var inactiveDirectReportLeaveRequest =
                new LeaveRequest
                {
                    Id =
                        inactiveDirectReportLeaveRequestId,

                    EmployeeId =
                        inactiveDirectReportId,

                    LeaveTypeId =
                        AnnualLeaveTypeId,

                    Reason =
                        "Inactive direct report collection scope request.",

                    CreatedAtUtc =
                        DateTime.UtcNow.AddMinutes(2),

                    UpdatedAtUtc =
                        null
                };

            inactiveDirectReportLeaveRequest.SetDateRange(
                new DateOnly(2026, 9, 20),
                new DateOnly(2026, 9, 21));

            var otherManagersLeaveRequest =
                new LeaveRequest
                {
                    Id =
                        otherManagersLeaveRequestId,

                    EmployeeId =
                        testData.OtherManagerId,

                    LeaveTypeId =
                        AnnualLeaveTypeId,

                    Reason =
                        "Other manager collection scope request.",

                    CreatedAtUtc =
                        DateTime.UtcNow.AddMinutes(3),

                    UpdatedAtUtc =
                        null
                };

            otherManagersLeaveRequest.SetDateRange(
                new DateOnly(2026, 10, 1),
                new DateOnly(2026, 10, 2));

            using (var scope =
                   _factory.Services.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                dbContext.Employees.Add(
                    inactiveDirectReport);

                dbContext.LeaveRequests.AddRange(
                    managerOwnLeaveRequest,
                    activeDirectReportLeaveRequest,
                    inactiveDirectReportLeaveRequest,
                    otherManagersLeaveRequest);

                await dbContext.SaveChangesAsync();
            }

            using var employeeResponse =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Get,
                    "/api/leave-requests",
                    testData.EmployeeAccessToken);

            Assert.Equal(
                HttpStatusCode.OK,
                employeeResponse.StatusCode);

            var employeeLeaveRequests =
                await employeeResponse.Content
                    .ReadFromJsonAsync<
                        List<LeaveRequestResponse>>(
                        JsonOptions);

            Assert.NotNull(
                employeeLeaveRequests);

            Assert.Contains(
                employeeLeaveRequests!,
                leaveRequest =>
                    leaveRequest.Id ==
                    activeDirectReportLeaveRequestId);

            Assert.DoesNotContain(
                employeeLeaveRequests,
                leaveRequest =>
                    leaveRequest.Id ==
                    managerOwnLeaveRequestId);

            Assert.DoesNotContain(
                employeeLeaveRequests,
                leaveRequest =>
                    leaveRequest.Id ==
                    inactiveDirectReportLeaveRequestId);

            Assert.DoesNotContain(
                employeeLeaveRequests,
                leaveRequest =>
                    leaveRequest.Id ==
                    otherManagersLeaveRequestId);

            using var managerResponse =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Get,
                    "/api/leave-requests",
                    testData.ManagerAccessToken);

            Assert.Equal(
                HttpStatusCode.OK,
                managerResponse.StatusCode);

            var managerLeaveRequests =
                await managerResponse.Content
                    .ReadFromJsonAsync<
                        List<LeaveRequestResponse>>(
                        JsonOptions);

            Assert.NotNull(
                managerLeaveRequests);

            Assert.Contains(
                managerLeaveRequests!,
                leaveRequest =>
                    leaveRequest.Id ==
                    activeDirectReportLeaveRequestId);

            Assert.DoesNotContain(
                managerLeaveRequests,
                leaveRequest =>
                    leaveRequest.Id ==
                    managerOwnLeaveRequestId);

            Assert.DoesNotContain(
                managerLeaveRequests,
                leaveRequest =>
                    leaveRequest.Id ==
                    inactiveDirectReportLeaveRequestId);

            Assert.DoesNotContain(
                managerLeaveRequests,
                leaveRequest =>
                    leaveRequest.Id ==
                    otherManagersLeaveRequestId);
        }
        finally
        {
            await CleanupAsync(
                testData.DepartmentId);
        }
    }

    [Fact]
    public async Task GetLeaveRequestById_EnforcesRoleScopedAccess()
    {
        await EnsureDatabaseReadyAsync();

        var testData =
            await CreateTeamAsync();

        var managerOwnLeaveRequestId =
            Guid.NewGuid();

        var activeDirectReportLeaveRequestId =
            Guid.NewGuid();

        var otherManagersLeaveRequestId =
            Guid.NewGuid();

        var inactiveDirectReportId =
            Guid.NewGuid();

        var inactiveDirectReportLeaveRequestId =
            Guid.NewGuid();

        try
        {
            var managerOwnLeaveRequest =
                new LeaveRequest
                {
                    Id =
                        managerOwnLeaveRequestId,

                    EmployeeId =
                        testData.ManagerId,

                    LeaveTypeId =
                        AnnualLeaveTypeId,

                    Reason =
                        "Manager own by-id scope request.",

                    CreatedAtUtc =
                        DateTime.UtcNow,

                    UpdatedAtUtc =
                        null
                };

            managerOwnLeaveRequest.SetDateRange(
                new DateOnly(2026, 11, 1),
                new DateOnly(2026, 11, 2));

            var activeDirectReportLeaveRequest =
                new LeaveRequest
                {
                    Id =
                        activeDirectReportLeaveRequestId,

                    EmployeeId =
                        testData.EmployeeId,

                    LeaveTypeId =
                        AnnualLeaveTypeId,

                    Reason =
                        "Active direct report by-id scope request.",

                    CreatedAtUtc =
                        DateTime.UtcNow.AddMinutes(1),

                    UpdatedAtUtc =
                        null
                };

            activeDirectReportLeaveRequest.SetDateRange(
                new DateOnly(2026, 11, 10),
                new DateOnly(2026, 11, 11));

            var otherManagersLeaveRequest =
                new LeaveRequest
                {
                    Id =
                        otherManagersLeaveRequestId,

                    EmployeeId =
                        testData.OtherManagerId,

                    LeaveTypeId =
                        AnnualLeaveTypeId,

                    Reason =
                        "Other manager by-id scope request.",

                    CreatedAtUtc =
                        DateTime.UtcNow.AddMinutes(2),

                    UpdatedAtUtc =
                        null
                };

            otherManagersLeaveRequest.SetDateRange(
                new DateOnly(2026, 11, 20),
                new DateOnly(2026, 11, 21));

            var inactiveDirectReport =
                new Employee
                {
                    Id =
                        inactiveDirectReportId,

                    FirstName =
                        "Inactive",

                    LastName =
                        "DirectReportById",

                    Email =
                        $"inactive.direct.report.byid.{Guid.NewGuid():N}@example.com",

                    DepartmentId =
                        testData.DepartmentId,

                    ManagerId =
                        testData.ManagerId,

                    Role =
                        EmployeeRole.Employee,

                    IsActive =
                        false,

                    CreatedAtUtc =
                        DateTime.UtcNow,

                    UpdatedAtUtc =
                        null
                };

            var inactiveDirectReportLeaveRequest =
                new LeaveRequest
                {
                    Id =
                        inactiveDirectReportLeaveRequestId,

                    EmployeeId =
                        inactiveDirectReportId,

                    LeaveTypeId =
                        AnnualLeaveTypeId,

                    Reason =
                        "Inactive direct report by-id scope request.",

                    CreatedAtUtc =
                        DateTime.UtcNow.AddMinutes(3),

                    UpdatedAtUtc =
                        null
                };

            inactiveDirectReportLeaveRequest.SetDateRange(
                new DateOnly(2026, 12, 1),
                new DateOnly(2026, 12, 2));

            using (var scope =
                   _factory.Services.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                dbContext.Employees.Add(
                    inactiveDirectReport);

                dbContext.LeaveRequests.AddRange(
                    managerOwnLeaveRequest,
                    activeDirectReportLeaveRequest,
                    otherManagersLeaveRequest,
                    inactiveDirectReportLeaveRequest);

                await dbContext.SaveChangesAsync();
            }

            using var employeeOwnResponse =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Get,
                    $"/api/leave-requests/{activeDirectReportLeaveRequestId}",
                    testData.EmployeeAccessToken);

            Assert.Equal(
                HttpStatusCode.OK,
                employeeOwnResponse.StatusCode);

            var employeeOwnLeaveRequest =
                await employeeOwnResponse.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeOwnLeaveRequest);

            Assert.Equal(
                activeDirectReportLeaveRequestId,
                employeeOwnLeaveRequest!.Id);

            using var employeeOutOfScopeResponse =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Get,
                    $"/api/leave-requests/{managerOwnLeaveRequestId}",
                    testData.EmployeeAccessToken);

            Assert.Equal(
                HttpStatusCode.NotFound,
                employeeOutOfScopeResponse.StatusCode);

            using var managerOwnResponse =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Get,
                    $"/api/leave-requests/{managerOwnLeaveRequestId}",
                    testData.ManagerAccessToken);

            Assert.Equal(
                HttpStatusCode.OK,
                managerOwnResponse.StatusCode);

            var managerOwnResult =
                await managerOwnResponse.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(
                managerOwnResult);

            Assert.Equal(
                managerOwnLeaveRequestId,
                managerOwnResult!.Id);

            using var managerDirectReportResponse =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Get,
                    $"/api/leave-requests/{activeDirectReportLeaveRequestId}",
                    testData.ManagerAccessToken);

            Assert.Equal(
                HttpStatusCode.OK,
                managerDirectReportResponse.StatusCode);

            var directReportResult =
                await managerDirectReportResponse.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(
                directReportResult);

            Assert.Equal(
                activeDirectReportLeaveRequestId,
                directReportResult!.Id);

            using var managerOutOfScopeResponse =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Get,
                    $"/api/leave-requests/{otherManagersLeaveRequestId}",
                    testData.ManagerAccessToken);

            Assert.Equal(
                HttpStatusCode.NotFound,
                managerOutOfScopeResponse.StatusCode);

            using var inactiveDirectReportResponse =
                await SendAuthorizedJsonRequestAsync(
                    HttpMethod.Get,
                    $"/api/leave-requests/{inactiveDirectReportLeaveRequestId}",
                    testData.ManagerAccessToken);

            Assert.Equal(
                HttpStatusCode.NotFound,
                inactiveDirectReportResponse.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                testData.DepartmentId);
        }
    }

    private async Task EnsureDatabaseReadyAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();

        var annualLeaveExists = await dbContext.LeaveTypes
            .AnyAsync(leaveType => leaveType.Id == AnnualLeaveTypeId);

        Assert.True(annualLeaveExists, "The default Annual Leave type should exist in the test database.");
    }

    private async Task<TestData> CreateTeamAsync()
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var passwordHashService =
            scope.ServiceProvider
                .GetRequiredService<IPasswordHashService>();

        var departmentId =
            Guid.NewGuid();

        var managerId =
            Guid.NewGuid();

        var employeeId =
            Guid.NewGuid();

        var otherManagerId =
            Guid.NewGuid();

        var hrEmployeeId =
            Guid.NewGuid();

        var suffix =
            Guid.NewGuid().ToString("N");

        var managerEmail =
            $"phase2.direct.manager.{suffix}@example.com";

        var otherManagerEmail =
            $"phase2.other.manager.{suffix}@example.com";

        var hrEmail =
            $"phase2.hr.{suffix}@example.com";

        var employeeEmail =
            $"phase2.employee.{suffix}@example.com";

        var department = new Department
        {
            Id =
                departmentId,

            Name =
                $"Phase 2 Test Department {suffix}",

            CreatedAtUtc =
                DateTime.UtcNow,

            UpdatedAtUtc =
                null
        };

        var manager = new Employee
        {
            Id =
                managerId,

            FirstName =
                "Direct",

            LastName =
                "Manager",

            Email =
                managerEmail,

            DepartmentId =
                departmentId,

            ManagerId =
                null,

            Role =
                EmployeeRole.Manager,

            IsActive =
                true,

            CreatedAtUtc =
                DateTime.UtcNow,

            UpdatedAtUtc =
                null
        };

        var otherManager = new Employee
        {
            Id =
                otherManagerId,

            FirstName =
                "Other",

            LastName =
                "Manager",

            Email =
                otherManagerEmail,

            DepartmentId =
                departmentId,

            ManagerId =
                null,

            Role =
                EmployeeRole.Manager,

            IsActive =
                true,

            CreatedAtUtc =
                DateTime.UtcNow,

            UpdatedAtUtc =
                null
        };

        var hrEmployee = new Employee
        {
            Id =
                hrEmployeeId,

            FirstName =
                "Test",

            LastName =
                "HR",

            Email =
                hrEmail,

            DepartmentId =
                departmentId,

            ManagerId =
                null,

            Role =
                EmployeeRole.HR,

            IsActive =
                true,

            CreatedAtUtc =
                DateTime.UtcNow,

            UpdatedAtUtc =
                null
        };

        var employee = new Employee
        {
            Id =
                employeeId,

            FirstName =
                "Test",

            LastName =
                "Employee",

            Email =
                employeeEmail,

            DepartmentId =
                departmentId,

            ManagerId =
                managerId,

            Role =
                EmployeeRole.Employee,

            IsActive =
                true,

            CreatedAtUtc =
                DateTime.UtcNow,

            UpdatedAtUtc =
                null
        };

        var managerAccount =
            new UserAccount(
                managerId,
                passwordHashService.HashPassword(
                    Password));

        var otherManagerAccount =
            new UserAccount(
                otherManagerId,
                passwordHashService.HashPassword(
                    Password));

        var hrAccount =
            new UserAccount(
                hrEmployeeId,
                passwordHashService.HashPassword(
                    Password));

        var employeeAccount =
            new UserAccount(
                employeeId,
                passwordHashService.HashPassword(
                    Password));

        dbContext.Departments.Add(
            department);

        dbContext.Employees.AddRange(
            manager,
            otherManager,
            hrEmployee,
            employee);

        dbContext.UserAccounts.AddRange(
            managerAccount,
            otherManagerAccount,
            hrAccount,
            employeeAccount);

        await dbContext.SaveChangesAsync();

        var managerAccessToken =
            await LoginAsync(
                managerEmail);

        var otherManagerAccessToken =
            await LoginAsync(
                otherManagerEmail);

        var hrAccessToken =
            await LoginAsync(
                hrEmail);

        var employeeAccessToken =
            await LoginAsync(
                employeeEmail);

        return new TestData(
            departmentId,
            managerId,
            employeeId,
            otherManagerId,
            hrEmployeeId,
            managerAccessToken,
            otherManagerAccessToken,
            employeeAccessToken,
            hrAccessToken);
    }

    private async Task<Guid> CreateZeroAllowanceLeaveTypeAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var leaveTypeId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N");

        var leaveType = new LeaveType
        {
            Id = leaveTypeId,
            Name = $"Zero Allowance Test Leave {suffix}",
            DefaultAnnualAllowanceDays = 0,
            IsPaid = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        dbContext.LeaveTypes.Add(leaveType);

        await dbContext.SaveChangesAsync();

        return leaveTypeId;
    }

    private async Task<HttpResponseMessage> SendAuthorizedJsonRequestAsync(
        HttpMethod method,
        string requestUri,
        string accessToken,
        object? content = null)
    {
        using var request =
            new HttpRequestMessage(
                method,
                requestUri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        if (content is not null)
        {
            request.Content =
                JsonContent.Create(
                    content);
        }

        return await _client.SendAsync(
            request);
    }

    private async Task<LeaveRequestResponse> CreateLeaveRequestAsync(
       TestData testData,
       string startDate,
       string endDate,
       string reason,
       Guid? leaveTypeId = null)
    {
        var request = new
        {
            leaveTypeId =
                leaveTypeId
                ?? AnnualLeaveTypeId,
            startDate,
            endDate,
            reason
        };

        using var response =
            await SendAuthorizedJsonRequestAsync(
                HttpMethod.Post,
                "/api/leave-requests",
                testData.EmployeeAccessToken,
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var leaveRequest =
            await response.Content
                .ReadFromJsonAsync<LeaveRequestResponse>(
                    JsonOptions);

        Assert.NotNull(
            leaveRequest);

        Assert.Equal(
            testData.EmployeeId,
            leaveRequest!.EmployeeId);

        return leaveRequest;
    }

    private async Task<LeaveRequestResponse> ApproveLeaveRequestAsync(
        Guid leaveRequestId,
        string accessToken,
        string managerComment,
        Guid? spoofedReviewerEmployeeId = null)
    {
        using var response =
            await SendAuthorizedReviewRequestAsync(
                leaveRequestId,
                "approve",
                accessToken,
                managerComment,
                spoofedReviewerEmployeeId);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var leaveRequest =
            await response.Content
                .ReadFromJsonAsync<LeaveRequestResponse>(
                    JsonOptions);

        Assert.NotNull(
            leaveRequest);

        return leaveRequest!;
    }

    private async Task<LeaveRequestResponse> RejectLeaveRequestAsync(
        Guid leaveRequestId,
        string accessToken,
        string managerComment)
    {
        using var response =
            await SendAuthorizedReviewRequestAsync(
                leaveRequestId,
                "reject",
                accessToken,
                managerComment);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var leaveRequest =
            await response.Content
                .ReadFromJsonAsync<LeaveRequestResponse>(
                    JsonOptions);

        Assert.NotNull(
            leaveRequest);

        return leaveRequest!;
    }

    private async Task<HttpResponseMessage> SendAuthorizedReviewRequestAsync(
        Guid leaveRequestId,
        string reviewAction,
        string accessToken,
        string managerComment,
        Guid? spoofedReviewerEmployeeId = null)
    {
        var reviewRequest =
            new Dictionary<string, object?>
            {
                ["managerComment"] =
                    managerComment
            };

        if (spoofedReviewerEmployeeId.HasValue)
        {
            reviewRequest["reviewerEmployeeId"] =
                spoofedReviewerEmployeeId.Value;
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/leave-requests/{leaveRequestId}/{reviewAction}")
            {
                Content =
                    JsonContent.Create(
                        reviewRequest)
            };

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return await _client.SendAsync(
            request);
    }

    private async Task<string> LoginAsync(
        string email)
    {
        using var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password =
                        Password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var loginResponse =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>(
                    JsonOptions);

        Assert.NotNull(
            loginResponse);

        Assert.False(
            string.IsNullOrWhiteSpace(
                loginResponse!.AccessToken));

        return loginResponse.AccessToken;
    }

    private async Task ChangeEmployeeManagerAsync(
        Guid employeeId,
        Guid managerId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var employee =
            await dbContext.Employees
                .SingleAsync(
                    currentEmployee =>
                        currentEmployee.Id ==
                        employeeId);

        employee.ManagerId =
            managerId;

        employee.UpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid?> GetReviewedByEmployeeIdAsync(
        Guid leaveRequestId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        return await dbContext.LeaveRequests
            .Where(
                leaveRequest =>
                    leaveRequest.Id ==
                    leaveRequestId)
            .Select(
                leaveRequest =>
                    leaveRequest.ReviewedByEmployeeId)
            .SingleAsync();
    }

    private static async Task AssertForbiddenProblemDetailsAsync(
        HttpResponseMessage response,
        string expectedInstance)
    {
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetailsResponse>(
                    JsonOptions);

        Assert.NotNull(
            problem);

        Assert.Equal(
            403,
            problem!.Status);

        Assert.Equal(
            "Forbidden.",
            problem.Title);

        Assert.Equal(
            "You do not have permission to perform this operation.",
            problem.Detail);

        Assert.Equal(
            expectedInstance,
            problem.Instance);

        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.TraceId));
    }

    private async Task<LeaveBalanceResponse> GetBalanceAsync(
        TestData testData,
        int year)
    {
        using var response =
            await SendAuthorizedJsonRequestAsync(
                HttpMethod.Get,
                "/api/leave-requests/balance" +
                $"?leaveTypeId={AnnualLeaveTypeId}" +
                $"&year={year}",
                testData.EmployeeAccessToken);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var balance =
            await response.Content
                .ReadFromJsonAsync<LeaveBalanceResponse>(
                    JsonOptions);

        Assert.NotNull(
            balance);

        Assert.Equal(
            testData.EmployeeId,
            balance!.EmployeeId);

        return balance;
    }

    private async Task CleanupAsync(
        Guid departmentId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var employeeIds =
            await dbContext.Employees
                .Where(
                    employee =>
                        employee.DepartmentId ==
                        departmentId)
                .Select(
                    employee =>
                        employee.Id)
                .ToListAsync();

        var leaveRequests =
            await dbContext.LeaveRequests
                .Where(
                    leaveRequest =>
                        employeeIds.Contains(
                            leaveRequest.EmployeeId))
                .ToListAsync();

        dbContext.LeaveRequests.RemoveRange(
            leaveRequests);

        var userAccounts =
            await dbContext.UserAccounts
                .Where(
                    userAccount =>
                        employeeIds.Contains(
                            userAccount.EmployeeId))
                .ToListAsync();

        dbContext.UserAccounts.RemoveRange(
            userAccounts);

        var employees =
            await dbContext.Employees
                .Where(
                    employee =>
                        employeeIds.Contains(
                            employee.Id))
                .ToListAsync();

        foreach (var employee in employees)
        {
            employee.ManagerId =
                null;
        }

        await dbContext.SaveChangesAsync();

        dbContext.Employees.RemoveRange(
            employees);

        var department =
            await dbContext.Departments
                .FirstOrDefaultAsync(
                    department =>
                        department.Id ==
                        departmentId);

        if (department is not null)
        {
            dbContext.Departments.Remove(
                department);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task CleanupLeaveTypeAsync(Guid leaveTypeId)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var leaveType = await dbContext.LeaveTypes
            .FirstOrDefaultAsync(leaveType => leaveType.Id == leaveTypeId);

        if (leaveType is not null)
        {
            dbContext.LeaveTypes.Remove(leaveType);
            await dbContext.SaveChangesAsync();
        }
    }

    private sealed record TestData(
        Guid DepartmentId,
        Guid ManagerId,
        Guid EmployeeId,
        Guid OtherManagerId,
        Guid HrEmployeeId,
        string ManagerAccessToken,
        string OtherManagerAccessToken,
        string EmployeeAccessToken,
        string HrAccessToken);

    private sealed record LeaveRequestResponse(
        Guid Id,
        Guid EmployeeId,
        Guid LeaveTypeId,
        int RequestedDays,
        LeaveRequestStatus Status,
        string Reason,
        string? ManagerComment,
        Guid? ReviewedByEmployeeId,
        string? ReviewedByEmployeeFullName);

    private sealed record LeaveBalanceResponse(
        Guid EmployeeId,
        Guid LeaveTypeId,
        string LeaveTypeName,
        int Year,
        int EntitledDays,
        int UsedDays,
        int RemainingDays);

    private sealed record ProblemDetailsResponse(
        string Title,
        int Status,
        string Detail,
        string? Instance,
        string? TraceId);
}