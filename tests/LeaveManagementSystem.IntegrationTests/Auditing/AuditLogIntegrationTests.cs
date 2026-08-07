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

namespace LeaveManagementSystem.IntegrationTests.Auditing;

public sealed class AuditLogIntegrationTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    private const string Password =
        "Audit-Integration-Test-Password-123!";

    [Fact]
    public async Task AuditedMutations_PersistExpectedScopeActorsActionsAndChangedProperties()
    {
        await EnsureDatabaseReadyAsync();

        var startedAtUtc =
            DateTime.UtcNow.AddSeconds(-1);

        var testData =
            await CreateTeamAsync();

        var leaveRequestIds =
            new List<Guid>();

        try
        {
            var approvedLeaveRequest =
                await CreateLeaveRequestAsync(
                    testData.EmployeeAccessToken,
                    new DateOnly(2026, 9, 10),
                    new DateOnly(2026, 9, 11),
                    "Audit approval request.");

            leaveRequestIds.Add(
                approvedLeaveRequest.Id);

            await ReviewLeaveRequestAsync(
                approvedLeaveRequest.Id,
                "approve",
                testData.ManagerAccessToken,
                "Approved by audit integration test.");

            var rejectedLeaveRequest =
                await CreateLeaveRequestAsync(
                    testData.EmployeeAccessToken,
                    new DateOnly(2026, 10, 10),
                    new DateOnly(2026, 10, 11),
                    "Audit rejection request.");

            leaveRequestIds.Add(
                rejectedLeaveRequest.Id);

            await ReviewLeaveRequestAsync(
                rejectedLeaveRequest.Id,
                "reject",
                testData.ManagerAccessToken,
                "Rejected by audit integration test.");

            var deletedLeaveRequest =
                await CreateLeaveRequestAsync(
                    testData.EmployeeAccessToken,
                    new DateOnly(2026, 11, 10),
                    new DateOnly(2026, 11, 11),
                    "Audit deletion request.");

            leaveRequestIds.Add(
                deletedLeaveRequest.Id);

            await DeleteLeaveRequestInSystemContextAsync(
                deletedLeaveRequest.Id);

            await DeactivateEmployeeInSystemContextAsync(
                testData.EmployeeId);

            var finishedAtUtc =
                DateTime.UtcNow.AddSeconds(1);

            using var scope =
                _factory.Services.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            var relevantEntityIds =
                new[]
                {
                    testData.DepartmentId,
                    testData.ManagerId,
                    testData.EmployeeId,
                    testData.ManagerUserAccountId,
                    testData.EmployeeUserAccountId,
                    approvedLeaveRequest.Id,
                    rejectedLeaveRequest.Id,
                    deletedLeaveRequest.Id
                };

            var auditLogs =
                await dbContext.AuditLogs
                    .AsNoTracking()
                    .Where(
                        auditLog =>
                            relevantEntityIds.Contains(
                                auditLog.EntityId))
                    .ToListAsync();

            Assert.Equal(
                9,
                auditLogs.Count);

            Assert.DoesNotContain(
                auditLogs,
                auditLog =>
                    auditLog.EntityName ==
                    nameof(Department));

            Assert.DoesNotContain(
                auditLogs,
                auditLog =>
                    auditLog.EntityName ==
                    nameof(UserAccount));

            Assert.All(
                auditLogs,
                auditLog =>
                    Assert.InRange(
                        auditLog.OccurredAtUtc,
                        startedAtUtc,
                        finishedAtUtc));

            var managerCreatedAudit =
                Assert.Single(
                    auditLogs,
                    auditLog =>
                        auditLog.EntityName ==
                            nameof(Employee)
                        && auditLog.EntityId ==
                            testData.ManagerId
                        && auditLog.Action ==
                            AuditAction.Created);

            Assert.Null(
                managerCreatedAudit.ActorEmployeeId);

            var employeeCreatedAudit =
                Assert.Single(
                    auditLogs,
                    auditLog =>
                        auditLog.EntityName ==
                            nameof(Employee)
                        && auditLog.EntityId ==
                            testData.EmployeeId
                        && auditLog.Action ==
                            AuditAction.Created);

            Assert.Null(
                employeeCreatedAudit.ActorEmployeeId);

            var employeeUpdatedAudit =
                Assert.Single(
                    auditLogs,
                    auditLog =>
                        auditLog.EntityName ==
                            nameof(Employee)
                        && auditLog.EntityId ==
                            testData.EmployeeId
                        && auditLog.Action ==
                            AuditAction.Updated);

            Assert.Null(
                employeeUpdatedAudit.ActorEmployeeId);

            Assert.Equal(
                "[\"IsActive\"]",
                employeeUpdatedAudit.ChangedPropertiesJson);

            AssertLeaveRequestAuditPair(
                auditLogs,
                approvedLeaveRequest.Id,
                AuditAction.Approved,
                testData.EmployeeId,
                testData.ManagerId);

            AssertLeaveRequestAuditPair(
                auditLogs,
                rejectedLeaveRequest.Id,
                AuditAction.Rejected,
                testData.EmployeeId,
                testData.ManagerId);

            var deletedRequestAudits =
                auditLogs
                    .Where(
                        auditLog =>
                            auditLog.EntityName ==
                                nameof(LeaveRequest)
                            && auditLog.EntityId ==
                                deletedLeaveRequest.Id)
                    .ToArray();

            Assert.Equal(
                2,
                deletedRequestAudits.Length);

            var deletedRequestCreatedAudit =
                Assert.Single(
                    deletedRequestAudits,
                    auditLog =>
                        auditLog.Action ==
                            AuditAction.Created);

            Assert.Equal(
                testData.EmployeeId,
                deletedRequestCreatedAudit.ActorEmployeeId);

            var deletedRequestDeletedAudit =
                Assert.Single(
                    deletedRequestAudits,
                    auditLog =>
                        auditLog.Action ==
                            AuditAction.Deleted);

            Assert.Null(
                deletedRequestDeletedAudit.ActorEmployeeId);

            Assert.DoesNotContain(
                "Audit deletion request.",
                deletedRequestDeletedAudit.ChangedPropertiesJson,
                StringComparison.Ordinal);
        }
        finally
        {
            await CleanupAuditTestDataAsync(
                testData,
                leaveRequestIds);
        }
    }

    private static void AssertLeaveRequestAuditPair(
        IReadOnlyCollection<AuditLog> auditLogs,
        Guid leaveRequestId,
        AuditAction reviewAction,
        Guid employeeId,
        Guid managerId)
    {
        var requestAudits =
            auditLogs
                .Where(
                    auditLog =>
                        auditLog.EntityName ==
                            nameof(LeaveRequest)
                        && auditLog.EntityId ==
                            leaveRequestId)
                .ToArray();

        Assert.Equal(
            2,
            requestAudits.Length);

        var createdAudit =
            Assert.Single(
                requestAudits,
                auditLog =>
                    auditLog.Action ==
                        AuditAction.Created);

        Assert.Equal(
            employeeId,
            createdAudit.ActorEmployeeId);

        var reviewAudit =
            Assert.Single(
                requestAudits,
                auditLog =>
                    auditLog.Action ==
                        reviewAction);

        Assert.Equal(
            managerId,
            reviewAudit.ActorEmployeeId);

        Assert.Equal(
            "[\"ManagerComment\",\"ReviewedAtUtc\",\"ReviewedByEmployeeId\",\"Status\"]",
            reviewAudit.ChangedPropertiesJson);

        Assert.DoesNotContain(
            "UpdatedAtUtc",
            reviewAudit.ChangedPropertiesJson,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "audit integration test",
            reviewAudit.ChangedPropertiesJson,
            StringComparison.OrdinalIgnoreCase);
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

        var suffix =
            Guid.NewGuid().ToString("N");

        var managerEmail =
            $"audit.manager.{suffix}@example.com";

        var employeeEmail =
            $"audit.employee.{suffix}@example.com";

        var department =
            new Department
            {
                Id = departmentId,
                Name =
                    $"Audit Integration Department {suffix}",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = null
            };

        var manager =
            new Employee
            {
                Id = managerId,
                FirstName = "Audit",
                LastName = "Manager",
                Email = managerEmail,
                DepartmentId = departmentId,
                ManagerId = null,
                Role = EmployeeRole.Manager,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = null
            };

        var employee =
            new Employee
            {
                Id = employeeId,
                FirstName = "Audit",
                LastName = "Employee",
                Email = employeeEmail,
                DepartmentId = departmentId,
                ManagerId = managerId,
                Role = EmployeeRole.Employee,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = null
            };

        var managerAccount =
            new UserAccount(
                managerId,
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
            employee);

        dbContext.UserAccounts.AddRange(
            managerAccount,
            employeeAccount);

        await dbContext.SaveChangesAsync();

        var managerAccessToken =
            await LoginAsync(
                managerEmail);

        var employeeAccessToken =
            await LoginAsync(
                employeeEmail);

        return new TestData(
            departmentId,
            managerId,
            employeeId,
            managerAccount.Id,
            employeeAccount.Id,
            managerAccessToken,
            employeeAccessToken);
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
                    password = Password
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

    private async Task<LeaveRequestResponse> CreateLeaveRequestAsync(
        string accessToken,
        DateOnly startDate,
        DateOnly endDate,
        string reason)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/leave-requests")
            {
                Content =
                    JsonContent.Create(
                        new
                        {
                            leaveTypeId =
                                AnnualLeaveTypeId,
                            startDate,
                            endDate,
                            reason
                        })
            };

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        using var response =
            await _client.SendAsync(
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

        return leaveRequest!;
    }

    private async Task ReviewLeaveRequestAsync(
        Guid leaveRequestId,
        string reviewAction,
        string accessToken,
        string managerComment)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/leave-requests/{leaveRequestId}/{reviewAction}")
            {
                Content =
                    JsonContent.Create(
                        new
                        {
                            managerComment
                        })
            };

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        using var response =
            await _client.SendAsync(
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    private async Task DeactivateEmployeeInSystemContextAsync(
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
                    currentEmployee =>
                        currentEmployee.Id ==
                        employeeId);

        employee.IsActive = false;
        employee.UpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    private async Task DeleteLeaveRequestInSystemContextAsync(
        Guid leaveRequestId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var leaveRequest =
            await dbContext.LeaveRequests
                .SingleAsync(
                    currentLeaveRequest =>
                        currentLeaveRequest.Id ==
                        leaveRequestId);

        dbContext.LeaveRequests.Remove(
            leaveRequest);

        await dbContext.SaveChangesAsync();
    }

    private async Task CleanupAuditTestDataAsync(
        TestData testData,
        IReadOnlyCollection<Guid> leaveRequestIds)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var auditEntityIds =
            leaveRequestIds
                .Concat(
                    new[]
                    {
                        testData.DepartmentId,
                        testData.ManagerId,
                        testData.EmployeeId,
                        testData.ManagerUserAccountId,
                        testData.EmployeeUserAccountId
                    })
                .Distinct()
                .ToArray();

        await dbContext.AuditLogs
            .Where(
                auditLog =>
                    auditEntityIds.Contains(
                        auditLog.EntityId))
            .ExecuteDeleteAsync();

        await dbContext.LeaveRequests
            .Where(
                leaveRequest =>
                    leaveRequest.EmployeeId ==
                    testData.EmployeeId)
            .ExecuteDeleteAsync();

        await dbContext.UserAccounts
            .Where(
                account =>
                    account.EmployeeId ==
                        testData.EmployeeId
                    || account.EmployeeId ==
                        testData.ManagerId)
            .ExecuteDeleteAsync();

        await dbContext.Employees
            .Where(
                employee =>
                    employee.Id ==
                    testData.EmployeeId)
            .ExecuteDeleteAsync();

        await dbContext.Employees
            .Where(
                employee =>
                    employee.Id ==
                    testData.ManagerId)
            .ExecuteDeleteAsync();

        await dbContext.Departments
            .Where(
                department =>
                    department.Id ==
                    testData.DepartmentId)
            .ExecuteDeleteAsync();
    }

    private sealed record TestData(
        Guid DepartmentId,
        Guid ManagerId,
        Guid EmployeeId,
        Guid ManagerUserAccountId,
        Guid EmployeeUserAccountId,
        string ManagerAccessToken,
        string EmployeeAccessToken);
}
