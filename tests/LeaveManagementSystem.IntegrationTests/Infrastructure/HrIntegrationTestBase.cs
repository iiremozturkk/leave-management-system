using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.Infrastructure;

public abstract class HrIntegrationTestBase
    : IntegrationTestBase,
      IAsyncLifetime
{
    private const string Password =
        "Hr-Integration-Test-Password-123!";

    private Guid _hrDepartmentId;

    private Guid _hrUserAccountId;

    protected HrIntegrationTestBase(
        TestWebApplicationFactory factory)
        : base(factory)
    {
        HrClient = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress =
                    new Uri("https://localhost")
            });
    }

    protected HttpClient HrClient { get; }

    protected Guid HrEmployeeId { get; private set; }

    protected override HttpClient EmployeeApiClient =>
        HrClient;

    public async Task InitializeAsync()
    {
        await EnsureDatabaseReadyAsync();

        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var passwordHashService =
            scope.ServiceProvider
                .GetRequiredService<IPasswordHashService>();

        _hrDepartmentId =
            Guid.NewGuid();

        HrEmployeeId =
            Guid.NewGuid();

        var email =
            $"integration.hr.{Guid.NewGuid():N}@example.com";

        var department =
            new Department
            {
                Id = _hrDepartmentId,
                Name =
                    $"Integration HR Department {Guid.NewGuid():N}",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = null
            };

        var employee =
            new Employee
            {
                Id = HrEmployeeId,
                FirstName = "Integration",
                LastName = "HrAdministrator",
                Email = email,
                DepartmentId = _hrDepartmentId,
                ManagerId = null,
                Role = EmployeeRole.HR,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = null
            };

        var passwordHash =
            passwordHashService.HashPassword(
                Password);

        var userAccount =
            new UserAccount(
                HrEmployeeId,
                passwordHash);

        _hrUserAccountId =
            userAccount.Id;

        dbContext.Departments.Add(
            department);

        dbContext.Employees.Add(
            employee);

        dbContext.UserAccounts.Add(
            userAccount);

        await dbContext.SaveChangesAsync();

        using var loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password = Password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>(
                    JsonOptions);

        Assert.NotNull(
            loginResult);

        Assert.False(
            string.IsNullOrWhiteSpace(
                loginResult!.AccessToken));

        Assert.Equal(
            HrEmployeeId,
            loginResult.EmployeeId);

        Assert.Equal(
            EmployeeRole.HR,
            loginResult.Role);

        HrClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);
    }

    public async Task DisposeAsync()
    {
        try
        {
            await CleanupHrAuthenticationDataAsync();
        }
        finally
        {
            HrClient.Dispose();
        }
    }

    private async Task CleanupHrAuthenticationDataAsync()
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var userAccount =
            await dbContext.UserAccounts
                .FirstOrDefaultAsync(
                    account =>
                        account.Id ==
                        _hrUserAccountId);

        if (userAccount is not null)
        {
            dbContext.UserAccounts.Remove(
                userAccount);
        }

        var employee =
            await dbContext.Employees
                .FirstOrDefaultAsync(
                    employee =>
                        employee.Id ==
                        HrEmployeeId);

        if (employee is not null)
        {
            dbContext.Employees.Remove(
                employee);
        }

        var department =
            await dbContext.Departments
                .FirstOrDefaultAsync(
                    department =>
                        department.Id ==
                        _hrDepartmentId);

        if (department is not null)
        {
            dbContext.Departments.Remove(
                department);
        }

        await dbContext.SaveChangesAsync();
    }
}
