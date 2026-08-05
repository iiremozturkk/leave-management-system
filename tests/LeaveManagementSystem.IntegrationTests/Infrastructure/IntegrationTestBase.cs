using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase
    : IClassFixture<TestWebApplicationFactory>
{
    protected static readonly Guid AnnualLeaveTypeId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    protected static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    protected readonly TestWebApplicationFactory _factory;
    protected readonly HttpClient _client;

    protected virtual HttpClient EmployeeApiClient =>
        _client;

    protected IntegrationTestBase(
        TestWebApplicationFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress =
                    new Uri("https://localhost")
            });
    }

    protected async Task EnsureDatabaseReadyAsync()
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();

        var annualLeaveExists =
            await dbContext.LeaveTypes.AnyAsync(
                leaveType =>
                    leaveType.Id ==
                    AnnualLeaveTypeId);

        Assert.True(
            annualLeaveExists,
            "The default Annual Leave type should exist in the test database.");
    }

    protected async Task<Guid> CreateDepartmentAsync()
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name =
                $"Integration Test Department {Guid.NewGuid():N}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        dbContext.Departments.Add(
            department);

        await dbContext.SaveChangesAsync();

        return department.Id;
    }

    protected async Task<Guid> CreateEmployeeViaApiAsync(
        Guid departmentId,
        EmployeeRole role = EmployeeRole.Employee)
    {
        var suffix =
            Guid.NewGuid().ToString("N");

        var createRequest = new
        {
            firstName = "Integration",
            lastName = "LeaveEmployee",
            email =
                $"integration.leave.employee.{suffix}@example.com",
            departmentId,
            managerId = (Guid?)null,
            role
        };

        var createResponse =
            await EmployeeApiClient.PostAsJsonAsync(
                "/api/employees",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdEmployee =
            await createResponse.Content
                .ReadFromJsonAsync<EmployeeResponse>(
                    JsonOptions);

        Assert.NotNull(
            createdEmployee);

        return createdEmployee!.Id;
    }

    protected async Task CleanupAsync(
        Guid? leaveRequestId,
        Guid? employeeId,
        Guid? departmentId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        if (leaveRequestId is not null)
        {
            var leaveRequest =
                await dbContext.LeaveRequests
                    .FirstOrDefaultAsync(
                        request =>
                            request.Id ==
                            leaveRequestId.Value);

            if (leaveRequest is not null)
            {
                dbContext.LeaveRequests.Remove(
                    leaveRequest);
            }
        }

        if (employeeId is not null)
        {
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

    private protected static void AssertBusinessRuleProblemDetails(
        ProblemDetailsResponse problem,
        string expectedDetail,
        string expectedInstance)
    {
        Assert.Equal(
            400,
            problem.Status);

        Assert.Equal(
            "A business rule was violated.",
            problem.Title);

        Assert.Equal(
            expectedDetail,
            problem.Detail);

        Assert.Equal(
            expectedInstance,
            problem.Instance);

        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.TraceId));
    }
}
