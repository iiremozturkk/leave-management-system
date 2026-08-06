using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.LeaveRequests;

public sealed class GetLeaveCalendarEndpointTests(
    TestWebApplicationFactory factory)
    : HrIntegrationTestBase(factory)
{
    private const string Password =
        "Calendar-Integration-Test-Password-123!";

    [Fact]
    public async Task GetLeaveCalendar_HrReturnsOnlyOverlappingRequestsIncludingBoundariesOrderedByDate()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId =
            null;

        var leftBoundaryRequestId =
            Guid.NewGuid();

        var insideRequestId =
            Guid.NewGuid();

        var rightBoundaryRequestId =
            Guid.NewGuid();

        var beforeRangeRequestId =
            Guid.NewGuid();

        var afterRangeRequestId =
            Guid.NewGuid();

        try
        {
            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            var leftBoundaryRequest =
                CreateLeaveRequest(
                    leftBoundaryRequestId,
                    employeeId.Value,
                    "Calendar left boundary request.",
                    new DateOnly(2026, 8, 8),
                    new DateOnly(2026, 8, 10));

            var insideRequest =
                CreateLeaveRequest(
                    insideRequestId,
                    employeeId.Value,
                    "Calendar inside request.",
                    new DateOnly(2026, 8, 12),
                    new DateOnly(2026, 8, 13));

            var rightBoundaryRequest =
                CreateLeaveRequest(
                    rightBoundaryRequestId,
                    employeeId.Value,
                    "Calendar right boundary request.",
                    new DateOnly(2026, 8, 20),
                    new DateOnly(2026, 8, 22));

            var beforeRangeRequest =
                CreateLeaveRequest(
                    beforeRangeRequestId,
                    employeeId.Value,
                    "Calendar before-range request.",
                    new DateOnly(2026, 8, 1),
                    new DateOnly(2026, 8, 9));

            var afterRangeRequest =
                CreateLeaveRequest(
                    afterRangeRequestId,
                    employeeId.Value,
                    "Calendar after-range request.",
                    new DateOnly(2026, 8, 21),
                    new DateOnly(2026, 8, 23));

            using (var scope =
                   _factory.Services.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                dbContext.LeaveRequests.AddRange(
                    leftBoundaryRequest,
                    insideRequest,
                    rightBoundaryRequest,
                    beforeRangeRequest,
                    afterRangeRequest);

                await dbContext.SaveChangesAsync();
            }

            using var response =
                await HrClient.GetAsync(
                    "/api/leave-requests/calendar" +
                    "?startDate=2026-08-10" +
                    "&endDate=2026-08-20");

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var calendarItems =
                await response.Content
                    .ReadFromJsonAsync<
                        List<LeaveCalendarItemResponse>>(
                        JsonOptions);

            Assert.NotNull(
                calendarItems);

            Assert.Contains(
                calendarItems!,
                item =>
                    item.Id ==
                    leftBoundaryRequestId);

            Assert.Contains(
                calendarItems,
                item =>
                    item.Id ==
                    insideRequestId);

            Assert.Contains(
                calendarItems,
                item =>
                    item.Id ==
                    rightBoundaryRequestId);

            Assert.DoesNotContain(
                calendarItems,
                item =>
                    item.Id ==
                    beforeRangeRequestId);

            Assert.DoesNotContain(
                calendarItems,
                item =>
                    item.Id ==
                    afterRangeRequestId);

            var leftBoundaryIndex =
                calendarItems.FindIndex(
                    item =>
                        item.Id ==
                        leftBoundaryRequestId);

            var insideIndex =
                calendarItems.FindIndex(
                    item =>
                        item.Id ==
                        insideRequestId);

            var rightBoundaryIndex =
                calendarItems.FindIndex(
                    item =>
                        item.Id ==
                        rightBoundaryRequestId);

            Assert.True(
                leftBoundaryIndex >= 0);

            Assert.True(
                insideIndex >= 0);

            Assert.True(
                rightBoundaryIndex >= 0);

            Assert.True(
                leftBoundaryIndex <
                insideIndex);

            Assert.True(
                insideIndex <
                rightBoundaryIndex);

            var projectedItem =
                calendarItems[insideIndex];

            Assert.Equal(
                insideRequestId,
                projectedItem.Id);

            Assert.Equal(
                employeeId.Value,
                projectedItem.EmployeeId);

            Assert.Equal(
                "Integration LeaveEmployee",
                projectedItem.EmployeeFullName);

            Assert.Equal(
                AnnualLeaveTypeId,
                projectedItem.LeaveTypeId);

            Assert.Equal(
                "Annual Leave",
                projectedItem.LeaveTypeName);

            Assert.Equal(
                new DateOnly(2026, 8, 12),
                projectedItem.StartDate);

            Assert.Equal(
                new DateOnly(2026, 8, 13),
                projectedItem.EndDate);

            Assert.Equal(
                2,
                projectedItem.RequestedDays);

            Assert.Equal(
                LeaveRequestStatus.Pending,
                projectedItem.Status);
        }
        finally
        {
            using (var scope =
                   _factory.Services.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                var requestIds =
                    new[]
                    {
                        leftBoundaryRequestId,
                        insideRequestId,
                        rightBoundaryRequestId,
                        beforeRangeRequestId,
                        afterRangeRequestId
                    };

                var leaveRequests =
                    await dbContext.LeaveRequests
                        .Where(
                            request =>
                                requestIds.Contains(
                                    request.Id))
                        .ToListAsync();

                if (leaveRequests.Count > 0)
                {
                    dbContext.LeaveRequests.RemoveRange(
                        leaveRequests);

                    await dbContext.SaveChangesAsync();
                }
            }

            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task GetLeaveCalendar_RolesReturnOnlyVisibleRequests()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        var createdUsers =
            new List<TestUserData>();

        var activeDirectReportRequestId =
            Guid.NewGuid();

        var managerRequestId =
            Guid.NewGuid();

        var inactiveDirectReportRequestId =
            Guid.NewGuid();

        var unrelatedEmployeeRequestId =
            Guid.NewGuid();

        try
        {
            var manager =
                await CreateTestUserAsync(
                    departmentId,
                    EmployeeRole.Manager,
                    managerId: null,
                    isActive: true,
                    lastName: "CalendarManager");

            createdUsers.Add(
                manager);

            var activeDirectReport =
                await CreateTestUserAsync(
                    departmentId,
                    EmployeeRole.Employee,
                    manager.EmployeeId,
                    isActive: true,
                    lastName: "ActiveDirectReport");

            createdUsers.Add(
                activeDirectReport);

            var inactiveDirectReport =
                await CreateTestUserAsync(
                    departmentId,
                    EmployeeRole.Employee,
                    manager.EmployeeId,
                    isActive: false,
                    lastName: "InactiveDirectReport");

            createdUsers.Add(
                inactiveDirectReport);

            var unrelatedEmployee =
                await CreateTestUserAsync(
                    departmentId,
                    EmployeeRole.Employee,
                    managerId: null,
                    isActive: true,
                    lastName: "UnrelatedEmployee");

            createdUsers.Add(
                unrelatedEmployee);

            var activeDirectReportRequest =
                CreateLeaveRequest(
                    activeDirectReportRequestId,
                    activeDirectReport.EmployeeId,
                    "Active direct report calendar request.",
                    new DateOnly(2026, 9, 10),
                    new DateOnly(2026, 9, 11));

            var managerRequest =
                CreateLeaveRequest(
                    managerRequestId,
                    manager.EmployeeId,
                    "Manager own calendar request.",
                    new DateOnly(2026, 9, 12),
                    new DateOnly(2026, 9, 13));

            var inactiveDirectReportRequest =
                CreateLeaveRequest(
                    inactiveDirectReportRequestId,
                    inactiveDirectReport.EmployeeId,
                    "Inactive direct report calendar request.",
                    new DateOnly(2026, 9, 14),
                    new DateOnly(2026, 9, 15));

            var unrelatedEmployeeRequest =
                CreateLeaveRequest(
                    unrelatedEmployeeRequestId,
                    unrelatedEmployee.EmployeeId,
                    "Unrelated employee calendar request.",
                    new DateOnly(2026, 9, 16),
                    new DateOnly(2026, 9, 17));

            using (var scope =
                   _factory.Services.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                dbContext.LeaveRequests.AddRange(
                    activeDirectReportRequest,
                    managerRequest,
                    inactiveDirectReportRequest,
                    unrelatedEmployeeRequest);

                await dbContext.SaveChangesAsync();
            }

            var activeEmployeeToken =
                await LoginAsync(
                    activeDirectReport);

            var managerToken =
                await LoginAsync(
                    manager);

            const string calendarPath =
                "/api/leave-requests/calendar" +
                "?startDate=2026-09-01" +
                "&endDate=2026-09-30";

            using var employeeResponse =
                await SendAuthorizedGetAsync(
                    calendarPath,
                    activeEmployeeToken);

            Assert.Equal(
                HttpStatusCode.OK,
                employeeResponse.StatusCode);

            var employeeCalendarItems =
                await employeeResponse.Content
                    .ReadFromJsonAsync<
                        List<LeaveCalendarItemResponse>>(
                        JsonOptions);

            Assert.NotNull(
                employeeCalendarItems);

            Assert.Contains(
                employeeCalendarItems!,
                item =>
                    item.Id ==
                    activeDirectReportRequestId);

            Assert.DoesNotContain(
                employeeCalendarItems,
                item =>
                    item.Id ==
                    managerRequestId);

            Assert.DoesNotContain(
                employeeCalendarItems,
                item =>
                    item.Id ==
                    inactiveDirectReportRequestId);

            Assert.DoesNotContain(
                employeeCalendarItems,
                item =>
                    item.Id ==
                    unrelatedEmployeeRequestId);

            Assert.All(
                employeeCalendarItems,
                item =>
                    Assert.Equal(
                        activeDirectReport.EmployeeId,
                        item.EmployeeId));

            using var managerResponse =
                await SendAuthorizedGetAsync(
                    calendarPath,
                    managerToken);

            Assert.Equal(
                HttpStatusCode.OK,
                managerResponse.StatusCode);

            var managerCalendarItems =
                await managerResponse.Content
                    .ReadFromJsonAsync<
                        List<LeaveCalendarItemResponse>>(
                        JsonOptions);

            Assert.NotNull(
                managerCalendarItems);

            Assert.Contains(
                managerCalendarItems!,
                item =>
                    item.Id ==
                    activeDirectReportRequestId);

            Assert.DoesNotContain(
                managerCalendarItems,
                item =>
                    item.Id ==
                    managerRequestId);

            Assert.DoesNotContain(
                managerCalendarItems,
                item =>
                    item.Id ==
                    inactiveDirectReportRequestId);

            Assert.DoesNotContain(
                managerCalendarItems,
                item =>
                    item.Id ==
                    unrelatedEmployeeRequestId);

            Assert.All(
                managerCalendarItems,
                item =>
                    Assert.Equal(
                        activeDirectReport.EmployeeId,
                        item.EmployeeId));

            using var hrResponse =
                await HrClient.GetAsync(
                    calendarPath);

            Assert.Equal(
                HttpStatusCode.OK,
                hrResponse.StatusCode);

            var hrCalendarItems =
                await hrResponse.Content
                    .ReadFromJsonAsync<
                        List<LeaveCalendarItemResponse>>(
                        JsonOptions);

            Assert.NotNull(
                hrCalendarItems);

            Assert.Contains(
                hrCalendarItems!,
                item =>
                    item.Id ==
                    activeDirectReportRequestId);

            Assert.Contains(
                hrCalendarItems,
                item =>
                    item.Id ==
                    managerRequestId);

            Assert.Contains(
                hrCalendarItems,
                item =>
                    item.Id ==
                    inactiveDirectReportRequestId);

            Assert.Contains(
                hrCalendarItems,
                item =>
                    item.Id ==
                    unrelatedEmployeeRequestId);
        }
        finally
        {
            await CleanupRoleScopeDataAsync(
                departmentId,
                new[]
                {
                    activeDirectReportRequestId,
                    managerRequestId,
                    inactiveDirectReportRequestId,
                    unrelatedEmployeeRequestId
                },
                createdUsers);
        }
    }

    [Fact]
    public async Task GetLeaveCalendar_EndDateBeforeStartDate_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await HrClient.GetAsync(
                "/api/leave-requests/calendar" +
                "?startDate=2026-08-20" +
                "&endDate=2026-08-10");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private async Task<TestUserData> CreateTestUserAsync(
        Guid departmentId,
        EmployeeRole role,
        Guid? managerId,
        bool isActive,
        string lastName)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var passwordHashService =
            scope.ServiceProvider
                .GetRequiredService<IPasswordHashService>();

        var employeeId =
            Guid.NewGuid();

        var email =
            $"integration.calendar.{Guid.NewGuid():N}@example.com";

        var employee =
            new Employee
            {
                Id =
                    employeeId,

                FirstName =
                    "Integration",

                LastName =
                    lastName,

                Email =
                    email,

                DepartmentId =
                    departmentId,

                ManagerId =
                    managerId,

                Role =
                    role,

                IsActive =
                    isActive,

                CreatedAtUtc =
                    DateTime.UtcNow,

                UpdatedAtUtc =
                    null
            };

        var passwordHash =
            passwordHashService.HashPassword(
                Password);

        var userAccount =
            new UserAccount(
                employeeId,
                passwordHash);

        dbContext.Employees.Add(
            employee);

        dbContext.UserAccounts.Add(
            userAccount);

        await dbContext.SaveChangesAsync();

        return new TestUserData(
            userAccount.Id,
            employeeId,
            email);
    }

    private async Task<string> LoginAsync(
        TestUserData testUser)
    {
        using var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email =
                        testUser.Email,

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

    private async Task<HttpResponseMessage> SendAuthorizedGetAsync(
        string requestPath,
        string accessToken)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                requestPath);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return await _client.SendAsync(
            request);
    }

    private async Task CleanupRoleScopeDataAsync(
        Guid departmentId,
        IReadOnlyCollection<Guid> leaveRequestIds,
        IReadOnlyCollection<TestUserData> testUsers)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var leaveRequests =
            await dbContext.LeaveRequests
                .Where(
                    request =>
                        leaveRequestIds.Contains(
                            request.Id))
                .ToListAsync();

        if (leaveRequests.Count > 0)
        {
            dbContext.LeaveRequests.RemoveRange(
                leaveRequests);

            await dbContext.SaveChangesAsync();
        }

        var userAccountIds =
            testUsers
                .Select(
                    user =>
                        user.UserAccountId)
                .ToArray();

        var userAccounts =
            await dbContext.UserAccounts
                .Where(
                    account =>
                        userAccountIds.Contains(
                            account.Id))
                .ToListAsync();

        if (userAccounts.Count > 0)
        {
            dbContext.UserAccounts.RemoveRange(
                userAccounts);

            await dbContext.SaveChangesAsync();
        }

        var employeeIds =
            testUsers
                .Select(
                    user =>
                        user.EmployeeId)
                .ToArray();

        var employees =
            await dbContext.Employees
                .Where(
                    employee =>
                        employeeIds.Contains(
                            employee.Id))
                .ToListAsync();

        if (employees.Count > 0)
        {
            foreach (var employee in employees)
            {
                employee.ManagerId =
                    null;
            }

            await dbContext.SaveChangesAsync();

            dbContext.Employees.RemoveRange(
                employees);

            await dbContext.SaveChangesAsync();
        }

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

            await dbContext.SaveChangesAsync();
        }
    }

    private static LeaveRequest CreateLeaveRequest(
        Guid leaveRequestId,
        Guid employeeId,
        string reason,
        DateOnly startDate,
        DateOnly endDate)
    {
        var leaveRequest =
            new LeaveRequest
            {
                Id =
                    leaveRequestId,

                EmployeeId =
                    employeeId,

                LeaveTypeId =
                    AnnualLeaveTypeId,

                Reason =
                    reason,

                CreatedAtUtc =
                    DateTime.UtcNow,

                UpdatedAtUtc =
                    null
            };

        leaveRequest.SetDateRange(
            startDate,
            endDate);

        return leaveRequest;
    }

    private sealed record TestUserData(
        Guid UserAccountId,
        Guid EmployeeId,
        string Email);
}
