using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Commands.Login;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.Authentication;

public sealed class LoginEndpointTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithTokenAndUserData()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            const string password =
                "Correct-Horse-Battery-Staple-123!";

            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    password);

            employeeId = testUser.EmployeeId;

            var request = new
            {
                email =
                    $"  {testUser.Email.ToUpperInvariant()}  ",
                password
            };

            var beforeLoginUtc =
                DateTime.UtcNow;

            using var response =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    request);

            var afterLoginUtc =
                DateTime.UtcNow;

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

            Assert.InRange(
                loginResponse.ExpiresAtUtc,
                beforeLoginUtc.AddMinutes(60),
                afterLoginUtc.AddMinutes(60));

            Assert.Equal(
                testUser.UserAccountId,
                loginResponse.UserAccountId);

            Assert.Equal(
                testUser.EmployeeId,
                loginResponse.EmployeeId);

            Assert.Equal(
                testUser.Email,
                loginResponse.Email);

            Assert.Equal(
                EmployeeRole.Employee,
                loginResponse.Role);
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
    public async Task Login_WrongPassword_ReturnsUnauthorized()
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
                    "Correct-Password-123!");

            employeeId = testUser.EmployeeId;

            var request = new
            {
                email = testUser.Email,
                password = "Wrong-Password-123!"
            };

            using var response =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    request);

            await AssertUnauthorizedProblemDetailsAsync(
                response);
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
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        await EnsureDatabaseReadyAsync();

        var request = new
        {
            email =
                $"unknown.{Guid.NewGuid():N}@example.com",
            password = "Some-Password-123!"
        };

        using var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        await AssertUnauthorizedProblemDetailsAsync(
            response);
    }

    [Fact]
    public async Task Login_InactiveUserAccount_ReturnsUnauthorized()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            const string password =
                "Correct-Password-123!";

            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    password,
                    isUserAccountActive: false);

            employeeId = testUser.EmployeeId;

            var request = new
            {
                email = testUser.Email,
                password
            };

            using var response =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    request);

            await AssertUnauthorizedProblemDetailsAsync(
                response);
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
    public async Task Login_InactiveEmployee_ReturnsUnauthorized()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            const string password =
                "Correct-Password-123!";

            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    password,
                    isEmployeeActive: false);

            employeeId = testUser.EmployeeId;

            var request = new
            {
                email = testUser.Email,
                password
            };

            using var response =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    request);

            await AssertUnauthorizedProblemDetailsAsync(
                response);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    private static async Task AssertUnauthorizedProblemDetailsAsync(
        HttpResponseMessage response)
    {
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetailsResponse>(
                    JsonOptions);

        Assert.NotNull(
            problem);

        Assert.Equal(
            401,
            problem!.Status);

        Assert.Equal(
            "Authentication failed.",
            problem.Title);

        Assert.Equal(
            "Invalid email or password.",
            problem.Detail);
    }

    private static async Task AssertValidationProblemDetailsAsync(
        HttpResponseMessage response,
        string expectedPropertyName,
        string expectedErrorMessage)
    {
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ValidationProblemDetailsResponse>(
                    JsonOptions);

        Assert.NotNull(
            problem);

        Assert.Equal(
            400,
            problem!.Status);

        Assert.Equal(
            "One or more validation errors occurred.",
            problem.Title);

        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.TraceId));

        var error =
            Assert.Single(
                problem.Errors);

        Assert.Equal(
            expectedPropertyName,
            error.Key);

        Assert.Equal(
            new[] { expectedErrorMessage },
            error.Value);
    }

    [Fact]
    public async Task Login_InvalidEmail_ReturnsBadRequest()
    {
        var request = new
        {
            email = "not-an-email",
            password = "Valid-Password-123!"
        };

        using var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        await AssertValidationProblemDetailsAsync(
            response,
            nameof(LoginCommand.Email),
            "Email must be a valid email address.");
    }

    [Fact]
    public async Task Login_WhitespacePassword_ReturnsBadRequest()
    {
        var request = new
        {
            email = "employee@example.com",
            password = "   "
        };

        using var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        await AssertValidationProblemDetailsAsync(
            response,
            nameof(LoginCommand.Password),
            "Password is required.");
    }

    [Fact]
    public async Task Login_EmptyRequestBody_ReturnsBadRequest()
    {
        using var content =
            new StringContent(
                string.Empty,
                Encoding.UTF8,
                "application/json");

        using var response =
            await _client.PostAsync(
                "/api/auth/login",
                content);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ValidationProblemDetailsResponse>(
                    JsonOptions);

        Assert.NotNull(
            problem);

        Assert.Equal(
            400,
            problem!.Status);

        Assert.Equal(
            "One or more validation errors occurred.",
            problem.Title);

        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.TraceId));

        Assert.NotEmpty(
            problem.Errors);
    }

    private async Task<TestUserData> CreateUserAccountAsync(
        Guid departmentId,
        string password,
        bool isEmployeeActive = true,
        bool isUserAccountActive = true)
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
            $"integration.login.{Guid.NewGuid():N}@example.com";

        var employee = new Employee
        {
            Id = employeeId,
            FirstName = "Integration",
            LastName = "LoginUser",
            Email = email,
            DepartmentId = departmentId,
            ManagerId = null,
            Role = EmployeeRole.Employee,
            IsActive = isEmployeeActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        var passwordHash =
            passwordHashService.HashPassword(
                password);

        var userAccount =
            new UserAccount(
                employeeId,
                passwordHash);

        if (!isUserAccountActive)
        {
            userAccount.Deactivate();
        }

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

    private sealed record TestUserData(
        Guid UserAccountId,
        Guid EmployeeId,
        string Email);
}
