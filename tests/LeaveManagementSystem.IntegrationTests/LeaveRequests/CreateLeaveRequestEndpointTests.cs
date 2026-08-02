using System.Net;
using System.Net.Http.Json;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.LeaveRequests;

public sealed class CreateLeaveRequestEndpointTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithLocationAndPersistsSingleTrimmedRequest()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            var startDate =
                new DateOnly(
                    2026,
                    7,
                    15);

            var endDate =
                new DateOnly(
                    2026,
                    7,
                    17);

            using var response =
                await PostCreateAsync(
                    employeeId.Value,
                    AnnualLeaveTypeId,
                    startDate,
                    endDate,
                    "  Integration test leave request.  ");

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);

            var createdLeaveRequest =
                await response.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(
                createdLeaveRequest);

            Assert.NotEqual(
                Guid.Empty,
                createdLeaveRequest!.Id);

            Assert.Equal(
                employeeId.Value,
                createdLeaveRequest.EmployeeId);

            Assert.Equal(
                "Integration LeaveEmployee",
                createdLeaveRequest.EmployeeFullName);

            Assert.Equal(
                AnnualLeaveTypeId,
                createdLeaveRequest.LeaveTypeId);

            Assert.Equal(
                "Annual Leave",
                createdLeaveRequest.LeaveTypeName);

            Assert.Equal(
                startDate,
                createdLeaveRequest.StartDate);

            Assert.Equal(
                endDate,
                createdLeaveRequest.EndDate);

            Assert.Equal(
                3,
                createdLeaveRequest.RequestedDays);

            Assert.Equal(
                LeaveRequestStatus.Pending,
                createdLeaveRequest.Status);

            Assert.Equal(
                "Integration test leave request.",
                createdLeaveRequest.Reason);

            Assert.Null(
                createdLeaveRequest.ManagerComment);

            Assert.Null(
                createdLeaveRequest.ReviewedAtUtc);

            Assert.Null(
                createdLeaveRequest.ReviewedByEmployeeId);

            Assert.Null(
                createdLeaveRequest.ReviewedByEmployeeFullName);

            Assert.NotEqual(
                default,
                createdLeaveRequest.CreatedAtUtc);

            Assert.Null(
                createdLeaveRequest.UpdatedAtUtc);

            AssertCreatedLocation(
                response,
                createdLeaveRequest.Id);

            using var scope =
                _factory.Services.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            var persistedLeaveRequests =
                await dbContext.LeaveRequests
                    .AsNoTracking()
                    .Where(
                        leaveRequest =>
                            leaveRequest.EmployeeId ==
                            employeeId.Value)
                    .ToListAsync();

            var persistedLeaveRequest =
                Assert.Single(
                    persistedLeaveRequests);

            Assert.Equal(
                createdLeaveRequest.Id,
                persistedLeaveRequest.Id);

            Assert.Equal(
                AnnualLeaveTypeId,
                persistedLeaveRequest.LeaveTypeId);

            Assert.Equal(
                startDate,
                persistedLeaveRequest.StartDate);

            Assert.Equal(
                endDate,
                persistedLeaveRequest.EndDate);

            Assert.Equal(
                3,
                persistedLeaveRequest.RequestedDays);

            Assert.Equal(
                LeaveRequestStatus.Pending,
                persistedLeaveRequest.Status);

            Assert.Equal(
                "Integration test leave request.",
                persistedLeaveRequest.Reason);
        }
        finally
        {
            await CleanupEmployeeGraphAsync(
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task Create_ReasonIsEmpty_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await PostCreateAsync(
                Guid.NewGuid(),
                AnnualLeaveTypeId,
                new DateOnly(2026, 7, 15),
                new DateOnly(2026, 7, 17),
                "   ");

        await AssertInvalidLeaveRequestProblemDetailsAsync(
            response,
            "Reason cannot be empty.");
    }

    [Fact]
    public async Task Create_ReasonExceedsMaximumLength_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await PostCreateAsync(
                Guid.NewGuid(),
                AnnualLeaveTypeId,
                new DateOnly(2026, 7, 15),
                new DateOnly(2026, 7, 17),
                new string(
                    'a',
                    501));

        await AssertInvalidLeaveRequestProblemDetailsAsync(
            response,
            "Reason cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Create_EndDateBeforeStartDate_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await PostCreateAsync(
                Guid.NewGuid(),
                AnnualLeaveTypeId,
                new DateOnly(2026, 7, 17),
                new DateOnly(2026, 7, 15),
                "Invalid date range.");

        await AssertInvalidLeaveRequestProblemDetailsAsync(
            response,
            "End date cannot be earlier than start date.");
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public async Task Create_YearIsOutsideSupportedRange_ReturnsBadRequest(
        int year)
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await PostCreateAsync(
                Guid.NewGuid(),
                AnnualLeaveTypeId,
                new DateOnly(year, 7, 15),
                new DateOnly(year, 7, 17),
                "Unsupported year.");

        await AssertInvalidLeaveRequestProblemDetailsAsync(
            response,
            "Year must be between 2000 and 2100.");
    }

    [Fact]
    public async Task Create_EmployeeIdIsEmpty_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await PostCreateAsync(
                Guid.Empty,
                AnnualLeaveTypeId,
                new DateOnly(2026, 7, 15),
                new DateOnly(2026, 7, 17),
                "Empty employee id.");

        await AssertInvalidLeaveRequestProblemDetailsAsync(
            response,
            "Employee id cannot be empty.");
    }

    [Fact]
    public async Task Create_LeaveTypeIdIsEmpty_ReturnsBadRequestWithoutPersisting()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            using var response =
                await PostCreateAsync(
                    employeeId.Value,
                    Guid.Empty,
                    new DateOnly(2026, 7, 15),
                    new DateOnly(2026, 7, 17),
                    "Empty leave type id.");

            await AssertInvalidLeaveRequestProblemDetailsAsync(
                response,
                "Leave type id cannot be empty.");

            await AssertNoLeaveRequestsForEmployeeAsync(
                employeeId.Value);
        }
        finally
        {
            await CleanupEmployeeGraphAsync(
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task Create_EmployeeDoesNotExist_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await PostCreateAsync(
                Guid.NewGuid(),
                AnnualLeaveTypeId,
                new DateOnly(2026, 7, 15),
                new DateOnly(2026, 7, 17),
                "Missing employee.");

        await AssertInvalidLeaveRequestProblemDetailsAsync(
            response,
            "Employee does not exist or is not active.");
    }

    [Fact]
    public async Task Create_EmployeeIsInactive_ReturnsBadRequestWithoutPersisting()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            await DeactivateEmployeeAsync(
                employeeId.Value);

            using var response =
                await PostCreateAsync(
                    employeeId.Value,
                    AnnualLeaveTypeId,
                    new DateOnly(2026, 7, 15),
                    new DateOnly(2026, 7, 17),
                    "Inactive employee.");

            await AssertInvalidLeaveRequestProblemDetailsAsync(
                response,
                "Employee does not exist or is not active.");

            await AssertNoLeaveRequestsForEmployeeAsync(
                employeeId.Value);
        }
        finally
        {
            await CleanupEmployeeGraphAsync(
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task Create_LeaveTypeDoesNotExist_ReturnsBadRequestWithoutPersisting()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            using var response =
                await PostCreateAsync(
                    employeeId.Value,
                    Guid.NewGuid(),
                    new DateOnly(2026, 7, 15),
                    new DateOnly(2026, 7, 17),
                    "Missing leave type.");

            await AssertInvalidLeaveRequestProblemDetailsAsync(
                response,
                "Leave type does not exist.");

            await AssertNoLeaveRequestsForEmployeeAsync(
                employeeId.Value);
        }
        finally
        {
            await CleanupEmployeeGraphAsync(
                employeeId,
                departmentId);
        }
    }

    private async Task<HttpResponseMessage> PostCreateAsync(
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        string reason)
    {
        var request = new
        {
            employeeId,
            leaveTypeId,
            startDate,
            endDate,
            reason
        };

        return await _client.PostAsJsonAsync(
            "/api/leave-requests",
            request);
    }

    private async Task DeactivateEmployeeAsync(
        Guid employeeId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var employee =
            await dbContext.Employees
                .SingleAsync(
                    employee =>
                        employee.Id == employeeId);

        employee.IsActive = false;

        await dbContext.SaveChangesAsync();
    }

    private async Task AssertNoLeaveRequestsForEmployeeAsync(
        Guid employeeId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var leaveRequestCount =
            await dbContext.LeaveRequests
                .AsNoTracking()
                .CountAsync(
                    leaveRequest =>
                        leaveRequest.EmployeeId ==
                        employeeId);

        Assert.Equal(
            0,
            leaveRequestCount);
    }

    private async Task CleanupEmployeeGraphAsync(
        Guid? employeeId,
        Guid? departmentId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        if (employeeId is not null)
        {
            var leaveRequests =
                await dbContext.LeaveRequests
                    .Where(
                        leaveRequest =>
                            leaveRequest.EmployeeId ==
                            employeeId.Value)
                    .ToListAsync();

            if (leaveRequests.Count > 0)
            {
                dbContext.LeaveRequests.RemoveRange(
                    leaveRequests);
            }

            var employee =
                await dbContext.Employees
                    .FirstOrDefaultAsync(
                        employee =>
                            employee.Id ==
                            employeeId.Value);

            if (employee is not null)
            {
                dbContext.Employees.Remove(
                    employee);
            }
        }

        if (departmentId is not null)
        {
            var department =
                await dbContext.Departments
                    .FirstOrDefaultAsync(
                        department =>
                            department.Id ==
                            departmentId.Value);

            if (department is not null)
            {
                dbContext.Departments.Remove(
                    department);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static void AssertCreatedLocation(
        HttpResponseMessage response,
        Guid leaveRequestId)
    {
        var location =
            response.Headers.Location;

        Assert.NotNull(
            location);

        var actualPath =
            location!.IsAbsoluteUri
                ? location.AbsolutePath
                : location.OriginalString;

        Assert.Equal(
            $"/api/leave-requests/{leaveRequestId}",
            actualPath);
    }

    private static async Task AssertInvalidLeaveRequestProblemDetailsAsync(
        HttpResponseMessage response,
        string expectedDetail)
    {
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

        Assert.Equal(
            expectedDetail,
            problem.Detail);
    }
}
