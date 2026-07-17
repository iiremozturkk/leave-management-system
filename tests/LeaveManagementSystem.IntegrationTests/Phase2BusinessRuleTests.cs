using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests;

public sealed class Phase2BusinessRuleTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly Guid AnnualLeaveTypeId = Guid.Parse("10000000-0000-0000-0000-000000000001");
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
                testData.EmployeeId,
                "2026-08-10",
                "2026-08-14",
                "First leave request.");

            var overlapRequest = new
            {
                employeeId = testData.EmployeeId,
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-08-12",
                endDate = "2026-08-13",
                reason = "Overlapping leave request."
            };

            var response = await _client.PostAsJsonAsync("/api/leave-requests", overlapRequest);

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

    [Fact]
    public async Task NonDirectManagerCannotApproveLeaveRequest_ReturnsForbidden()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData.EmployeeId,
                "2026-08-10",
                "2026-08-14",
                "Approval test leave request.");

            var reviewRequest = new
            {
                reviewerEmployeeId = testData.OtherManagerId,
                managerComment = "Trying to approve as another manager."
            };

            var response = await _client.PostAsJsonAsync(
                $"/api/leave-requests/{leaveRequest.Id}/approve",
                reviewRequest);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonOptions);

            Assert.NotNull(problem);
            Assert.Equal(403, problem!.Status);
            Assert.Equal("Forbidden leave request operation.", problem.Title);
            Assert.Contains("direct manager", problem.Detail);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Fact]
    public async Task EmployeeCannotApproveLeaveRequest_ReturnsForbidden()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData.EmployeeId,
                "2026-08-10",
                "2026-08-14",
                "Employee approval test leave request.");

            var reviewRequest = new
            {
                reviewerEmployeeId = testData.EmployeeId,
                managerComment = "Trying to approve as employee."
            };

            var response = await _client.PostAsJsonAsync(
                $"/api/leave-requests/{leaveRequest.Id}/approve",
                reviewRequest);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonOptions);

            Assert.NotNull(problem);
            Assert.Equal(403, problem!.Status);
            Assert.Equal("Forbidden leave request operation.", problem.Title);
            Assert.Contains("Only managers can review", problem.Detail);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Fact]
    public async Task HrEmployeeCannotApproveLeaveRequest_ReturnsForbidden()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData.EmployeeId,
                "2026-08-10",
                "2026-08-14",
                "HR approval test leave request.");

            var reviewRequest = new
            {
                reviewerEmployeeId = testData.HrEmployeeId,
                managerComment = "Trying to approve as HR."
            };

            var response = await _client.PostAsJsonAsync(
                $"/api/leave-requests/{leaveRequest.Id}/approve",
                reviewRequest);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonOptions);

            Assert.NotNull(problem);
            Assert.Equal(403, problem!.Status);
            Assert.Equal("Forbidden leave request operation.", problem.Title);
            Assert.Contains("Only managers can review", problem.Detail);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
        }
    }

    [Fact]
    public async Task DirectManagerCanApproveLeaveRequest_AndBalanceIsUpdated()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var balanceBefore = await GetBalanceAsync(testData.EmployeeId, 2026);

            Assert.Equal(2026, balanceBefore.Year);
            Assert.Equal(20, balanceBefore.EntitledDays);
            Assert.Equal(0, balanceBefore.UsedDays);
            Assert.Equal(20, balanceBefore.RemainingDays);

            var leaveRequest = await CreateLeaveRequestAsync(
                testData.EmployeeId,
                "2026-08-10",
                "2026-08-14",
                "Manager approval test leave request.");

            var approvedLeaveRequest = await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerId,
                "Approved by direct manager.");

            Assert.Equal(LeaveRequestStatus.Approved, approvedLeaveRequest.Status);
            Assert.Equal(testData.ManagerId, approvedLeaveRequest.ReviewedByEmployeeId);
            Assert.Equal("Approved by direct manager.", approvedLeaveRequest.ManagerComment);

            var balanceAfter = await GetBalanceAsync(testData.EmployeeId, 2026);

            Assert.Equal(2026, balanceAfter.Year);
            Assert.Equal(20, balanceAfter.EntitledDays);
            Assert.Equal(5, balanceAfter.UsedDays);
            Assert.Equal(15, balanceAfter.RemainingDays);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
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
                testData.EmployeeId,
                "2026-08-10",
                "2026-08-14",
                "Approved leave request.");

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerId,
                "Approved by direct manager.");

            var exceedBalanceRequest = new
            {
                employeeId = testData.EmployeeId,
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-09-01",
                endDate = "2026-09-30",
                reason = "Exceed balance test."
            };

            var response = await _client.PostAsJsonAsync("/api/leave-requests", exceedBalanceRequest);

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
                testData.EmployeeId,
                "2026-11-01",
                "2026-11-20",
                "Request exactly the remaining annual balance.");

            Assert.Equal(20, leaveRequest.RequestedDays);

            var approvedLeaveRequest = await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerId,
                "Approved exact remaining balance.");

            Assert.Equal(LeaveRequestStatus.Approved, approvedLeaveRequest.Status);

            var balanceAfter = await GetBalanceAsync(testData.EmployeeId, 2026);

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
                testData.EmployeeId,
                "2025-08-10",
                "2025-08-14",
                "Previous year approved leave request.");

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerId,
                "Approved previous year leave.");

            var previousYearBalance = await GetBalanceAsync(testData.EmployeeId, 2025);

            Assert.Equal(2025, previousYearBalance.Year);
            Assert.Equal(20, previousYearBalance.EntitledDays);
            Assert.Equal(5, previousYearBalance.UsedDays);
            Assert.Equal(15, previousYearBalance.RemainingDays);

            var currentYearBalance = await GetBalanceAsync(testData.EmployeeId, 2026);

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
                testData.EmployeeId,
                "2026-12-30",
                "2027-01-02",
                "Cross-year leave request.");

            Assert.Equal(4, leaveRequest.RequestedDays);

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerId,
                "Approved cross-year leave.");

            var balance2026 = await GetBalanceAsync(testData.EmployeeId, 2026);

            Assert.Equal(2026, balance2026.Year);
            Assert.Equal(20, balance2026.EntitledDays);
            Assert.Equal(2, balance2026.UsedDays);
            Assert.Equal(18, balance2026.RemainingDays);

            var balance2027 = await GetBalanceAsync(testData.EmployeeId, 2027);

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
                testData.EmployeeId,
                "2026-01-01",
                "2026-01-19",
                "Existing approved leave request.");

            await ApproveLeaveRequestAsync(
                existingLeaveRequest.Id,
                testData.ManagerId,
                "Approved existing leave request.");

            var crossYearRequest = new
            {
                employeeId = testData.EmployeeId,
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-12-30",
                endDate = "2027-01-02",
                reason = "Cross-year leave exceeding one year balance."
            };

            var response = await _client.PostAsJsonAsync("/api/leave-requests", crossYearRequest);

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
                testData.EmployeeId,
                "2026-01-01",
                "2026-03-31",
                "Zero allowance leave type should skip balance check.",
                zeroAllowanceLeaveTypeId);

            Assert.Equal(zeroAllowanceLeaveTypeId, leaveRequest.LeaveTypeId);
            Assert.Equal(90, leaveRequest.RequestedDays);
            Assert.Equal(LeaveRequestStatus.Pending, leaveRequest.Status);
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
                testData.EmployeeId,
                "2026-10-01",
                "2026-10-03",
                "Approved leave request should not be reviewed again.");

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerId,
                "Approved by direct manager.");

            var reviewRequest = new
            {
                reviewerEmployeeId = testData.ManagerId,
                managerComment = "Trying to reject an already approved request."
            };

            var response = await _client.PostAsJsonAsync(
                $"/api/leave-requests/{leaveRequest.Id}/reject",
                reviewRequest);

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
    public async Task ApprovedLeaveRequestCannotBeUpdated_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        var testData = await CreateTeamAsync();

        try
        {
            var leaveRequest = await CreateLeaveRequestAsync(
                testData.EmployeeId,
                "2026-10-01",
                "2026-10-03",
                "Approved leave request should not be updated.");

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerId,
                "Approved by direct manager.");

            var updateRequest = new
            {
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-10-05",
                endDate = "2026-10-07",
                reason = "Trying to update an approved leave request."
            };

            var response = await _client.PutAsJsonAsync(
                $"/api/leave-requests/{leaveRequest.Id}",
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
                testData.EmployeeId,
                "2026-10-01",
                "2026-10-03",
                "Approved leave request should not be deleted.");

            await ApproveLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerId,
                "Approved by direct manager.");

            var response = await _client.DeleteAsync($"/api/leave-requests/{leaveRequest.Id}");

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
                testData.EmployeeId,
                "2026-10-01",
                "2026-10-03",
                "Rejected leave request should not block same dates.");

            await RejectLeaveRequestAsync(
                rejectedLeaveRequest.Id,
                testData.ManagerId,
                "Rejected by direct manager.");

            var newLeaveRequest = await CreateLeaveRequestAsync(
                testData.EmployeeId,
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
            var reviewRequest = new
            {
                reviewerEmployeeId = testData.ManagerId,
                managerComment = "Trying to approve a non-existent leave request."
            };

            var response = await _client.PostAsJsonAsync(
                $"/api/leave-requests/{Guid.NewGuid()}/approve",
                reviewRequest);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
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
                testData.EmployeeId,
                "2026-10-01",
                "2026-10-03",
                "Reject flow test leave request.");

            var rejectedLeaveRequest = await RejectLeaveRequestAsync(
                leaveRequest.Id,
                testData.ManagerId,
                "Rejected by direct manager.");

            Assert.Equal(LeaveRequestStatus.Rejected, rejectedLeaveRequest.Status);
            Assert.Equal(testData.ManagerId, rejectedLeaveRequest.ReviewedByEmployeeId);
            Assert.Equal("Rejected by direct manager.", rejectedLeaveRequest.ManagerComment);
        }
        finally
        {
            await CleanupAsync(testData.DepartmentId);
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
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var departmentId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var otherManagerId = Guid.NewGuid();
        var hrEmployeeId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N");

        var department = new Department
        {
            Id = departmentId,
            Name = $"Phase 2 Test Department {suffix}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        var manager = new Employee
        {
            Id = managerId,
            FirstName = "Direct",
            LastName = "Manager",
            Email = $"phase2.direct.manager.{suffix}@example.com",
            DepartmentId = departmentId,
            ManagerId = null,
            Role = EmployeeRole.Manager,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        var otherManager = new Employee
        {
            Id = otherManagerId,
            FirstName = "Other",
            LastName = "Manager",
            Email = $"phase2.other.manager.{suffix}@example.com",
            DepartmentId = departmentId,
            ManagerId = null,
            Role = EmployeeRole.Manager,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        var hrEmployee = new Employee
        {
            Id = hrEmployeeId,
            FirstName = "Test",
            LastName = "HR",
            Email = $"phase2.hr.{suffix}@example.com",
            DepartmentId = departmentId,
            ManagerId = null,
            Role = EmployeeRole.HR,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        var employee = new Employee
        {
            Id = employeeId,
            FirstName = "Test",
            LastName = "Employee",
            Email = $"phase2.employee.{suffix}@example.com",
            DepartmentId = departmentId,
            ManagerId = managerId,
            Role = EmployeeRole.Employee,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        dbContext.Departments.Add(department);
        dbContext.Employees.AddRange(manager, otherManager, hrEmployee, employee);

        await dbContext.SaveChangesAsync();

        return new TestData(departmentId, managerId, employeeId, otherManagerId, hrEmployeeId);
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

    private async Task<LeaveRequestResponse> CreateLeaveRequestAsync(
        Guid employeeId,
        string startDate,
        string endDate,
        string reason,
        Guid? leaveTypeId = null)
    {
        var request = new
        {
            employeeId,
            leaveTypeId = leaveTypeId ?? AnnualLeaveTypeId,
            startDate,
            endDate,
            reason
        };

        var response = await _client.PostAsJsonAsync("/api/leave-requests", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var leaveRequest = await response.Content.ReadFromJsonAsync<LeaveRequestResponse>(JsonOptions);

        Assert.NotNull(leaveRequest);

        return leaveRequest!;
    }

    private async Task<LeaveRequestResponse> ApproveLeaveRequestAsync(
        Guid leaveRequestId,
        Guid reviewerEmployeeId,
        string managerComment)
    {
        var reviewRequest = new
        {
            reviewerEmployeeId,
            managerComment
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/leave-requests/{leaveRequestId}/approve",
            reviewRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var leaveRequest = await response.Content.ReadFromJsonAsync<LeaveRequestResponse>(JsonOptions);

        Assert.NotNull(leaveRequest);

        return leaveRequest!;
    }

    private async Task<LeaveRequestResponse> RejectLeaveRequestAsync(
        Guid leaveRequestId,
        Guid reviewerEmployeeId,
        string managerComment)
    {
        var reviewRequest = new
        {
            reviewerEmployeeId,
            managerComment
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/leave-requests/{leaveRequestId}/reject",
            reviewRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var leaveRequest = await response.Content.ReadFromJsonAsync<LeaveRequestResponse>(JsonOptions);

        Assert.NotNull(leaveRequest);

        return leaveRequest!;
    }

    private async Task<LeaveBalanceResponse> GetBalanceAsync(Guid employeeId, int year)
    {
        var response = await _client.GetAsync(
            $"/api/leave-requests/balance?employeeId={employeeId}&leaveTypeId={AnnualLeaveTypeId}&year={year}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balance = await response.Content.ReadFromJsonAsync<LeaveBalanceResponse>(JsonOptions);

        Assert.NotNull(balance);

        return balance!;
    }

    private async Task CleanupAsync(Guid departmentId)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var employeeIds = await dbContext.Employees
            .Where(employee => employee.DepartmentId == departmentId)
            .Select(employee => employee.Id)
            .ToListAsync();

        var leaveRequests = await dbContext.LeaveRequests
            .Where(leaveRequest => employeeIds.Contains(leaveRequest.EmployeeId))
            .ToListAsync();

        dbContext.LeaveRequests.RemoveRange(leaveRequests);

        var employees = await dbContext.Employees
            .Where(employee => employeeIds.Contains(employee.Id))
            .ToListAsync();

        foreach (var employee in employees)
        {
            employee.ManagerId = null;
        }

        await dbContext.SaveChangesAsync();

        dbContext.Employees.RemoveRange(employees);

        var department = await dbContext.Departments
            .FirstOrDefaultAsync(department => department.Id == departmentId);

        if (department is not null)
        {
            dbContext.Departments.Remove(department);
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
        Guid HrEmployeeId);

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
        string Detail);
}