using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.Authorization;

public sealed class AuthorizationPolicyEndpointTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    private const string Password =
        "Authorization-Policy-Test-Password-123!";

    private const string AuthenticatedEmployeePath =
        "/api/test-authentication/authenticated-employee";

    private const string HrOnlyPath =
        "/api/test-authentication/hr-only";

    private const string ManagerOnlyPath =
        "/api/test-authentication/manager-only";

    private const string EmployeeCollectionPath =
        "/api/employees";

    [Fact]
    public async Task GetAuthenticatedEmployee_WithoutToken_ReturnsUnauthorized()
    {
        using var response =
            await _client.GetAsync(
                AuthenticatedEmployeePath);

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

    [Fact]
    public async Task GetAuthenticatedEmployee_WithValidEmployeeToken_ReturnsNoContent()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    EmployeeRole.Employee);

            employeeId =
                testUser.EmployeeId;

            var accessToken =
                await LoginAsync(
                    testUser);

            using var response =
                await SendAuthorizedGetAsync(
                    AuthenticatedEmployeePath,
                    accessToken);

            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task GetHrOnly_WithEmployeeToken_ReturnsSafeForbiddenProblemDetails()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    EmployeeRole.Employee);

            employeeId =
                testUser.EmployeeId;

            var accessToken =
                await LoginAsync(
                    testUser);

            using var response =
                await SendAuthorizedGetAsync(
                    HrOnlyPath,
                    accessToken);

            await AssertForbiddenProblemDetailsAsync(
                response,
                HrOnlyPath);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task GetHrOnly_WithValidHrToken_ReturnsNoContent()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    EmployeeRole.HR);

            employeeId =
                testUser.EmployeeId;

            var accessToken =
                await LoginAsync(
                    testUser);

            using var response =
                await SendAuthorizedGetAsync(
                    HrOnlyPath,
                    accessToken);

            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task GetEmployees_WithoutToken_ReturnsUnauthorized()
    {
        using var response =
            await _client.GetAsync(
                EmployeeCollectionPath);

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

    [Theory]
    [InlineData(EmployeeRole.Employee)]
    [InlineData(EmployeeRole.Manager)]
    public async Task GetEmployees_WithNonHrToken_ReturnsSafeForbiddenProblemDetails(
        EmployeeRole role)
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    role);

            employeeId =
                testUser.EmployeeId;

            var accessToken =
                await LoginAsync(
                    testUser);

            using var response =
                await SendAuthorizedGetAsync(
                    EmployeeCollectionPath,
                    accessToken);

            await AssertForbiddenProblemDetailsAsync(
                response,
                EmployeeCollectionPath);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task GetEmployees_WithValidHrToken_ReturnsOk()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    EmployeeRole.HR);

            employeeId =
                testUser.EmployeeId;

            var accessToken =
                await LoginAsync(
                    testUser);

            using var response =
                await SendAuthorizedGetAsync(
                    EmployeeCollectionPath,
                    accessToken);

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task GetManagerOnly_WithEmployeeToken_ReturnsSafeForbiddenProblemDetails()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    EmployeeRole.Employee);

            employeeId =
                testUser.EmployeeId;

            var accessToken =
                await LoginAsync(
                    testUser);

            using var response =
                await SendAuthorizedGetAsync(
                    ManagerOnlyPath,
                    accessToken);

            await AssertForbiddenProblemDetailsAsync(
                response,
                ManagerOnlyPath);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task GetManagerOnly_WithValidManagerToken_ReturnsNoContent()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    EmployeeRole.Manager);

            employeeId =
                testUser.EmployeeId;

            var accessToken =
                await LoginAsync(
                    testUser);

            using var response =
                await SendAuthorizedGetAsync(
                    ManagerOnlyPath,
                    accessToken);

            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task GetAuthenticatedEmployee_WhenUserAccountDeactivatedAfterTokenIssued_ReturnsSafeForbiddenProblemDetails()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    EmployeeRole.Employee);

            employeeId =
                testUser.EmployeeId;

            var accessToken =
                await LoginAsync(
                    testUser);

            await DeactivateUserAccountAsync(
                testUser.UserAccountId);

            using var response =
                await SendAuthorizedGetAsync(
                    AuthenticatedEmployeePath,
                    accessToken);

            await AssertForbiddenProblemDetailsAsync(
                response,
                AuthenticatedEmployeePath);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task GetAuthenticatedEmployee_WhenEmployeeDeactivatedAfterTokenIssued_ReturnsSafeForbiddenProblemDetails()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    EmployeeRole.Employee);

            employeeId =
                testUser.EmployeeId;

            var accessToken =
                await LoginAsync(
                    testUser);

            await DeactivateEmployeeAsync(
                testUser.EmployeeId);

            using var response =
                await SendAuthorizedGetAsync(
                    AuthenticatedEmployeePath,
                    accessToken);

            await AssertForbiddenProblemDetailsAsync(
                response,
                AuthenticatedEmployeePath);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task GetAuthenticatedEmployee_WhenEmployeeRoleChangedAfterTokenIssued_ReturnsSafeForbiddenProblemDetails()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    EmployeeRole.Employee);

            employeeId =
                testUser.EmployeeId;

            var accessToken =
                await LoginAsync(
                    testUser);

            await ChangeEmployeeRoleAsync(
                testUser.EmployeeId,
                EmployeeRole.Manager);

            using var response =
                await SendAuthorizedGetAsync(
                    AuthenticatedEmployeePath,
                    accessToken);

            await AssertForbiddenProblemDetailsAsync(
                response,
                AuthenticatedEmployeePath);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    private async Task<TestUserData> CreateUserAccountAsync(
        Guid departmentId,
        EmployeeRole role)
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
            $"integration.authorization.{Guid.NewGuid():N}@example.com";

        var employee =
            new Employee
            {
                Id =
                    employeeId,

                FirstName =
                    "Integration",

                LastName =
                    "AuthorizationUser",

                Email =
                    email,

                DepartmentId =
                    departmentId,

                ManagerId =
                    null,

                Role =
                    role,

                IsActive =
                    true,

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
        var request = new
        {
            email =
                testUser.Email,

            password =
                Password
        };

        using var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                request);

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

    private async Task DeactivateUserAccountAsync(
        Guid userAccountId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var userAccount =
            await dbContext.UserAccounts
                .SingleAsync(
                    account =>
                        account.Id ==
                        userAccountId);

        userAccount.Deactivate();

        await dbContext.SaveChangesAsync();
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
                    currentEmployee =>
                        currentEmployee.Id ==
                        employeeId);

        employee.IsActive =
            false;

        employee.UpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    private async Task ChangeEmployeeRoleAsync(
        Guid employeeId,
        EmployeeRole newRole)
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

        employee.Role =
            newRole;

        employee.UpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
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
            StatusCodes.Status403Forbidden,
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

    private sealed record TestUserData(
        Guid UserAccountId,
        Guid EmployeeId,
        string Email);
}
