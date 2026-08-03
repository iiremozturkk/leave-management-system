using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using LeaveManagementSystem.IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.Authentication;

public sealed class BearerAuthenticationEndpointTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetClaims_WithoutToken_ReturnsUnauthorized()
    {
        using var response =
            await _client.GetAsync(
                "/api/test-authentication/claims");

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
    public async Task GetClaims_WithMalformedToken_ReturnsUnauthorized()
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/test-authentication/claims");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                "this-is-not-a-valid-jwt");

        using var response =
            await _client.SendAsync(
                request);

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
    public async Task GetClaims_WithValidLoginToken_ReturnsAuthenticatedClaims()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            const string password =
                "Bearer-Integration-Test-Password-123!";

            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    password);

            employeeId = testUser.EmployeeId;

            var loginRequest = new
            {
                email = testUser.Email,
                password
            };

            using var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    loginRequest);

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

            using var claimsRequest =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "/api/test-authentication/claims");

            claimsRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    loginResult.AccessToken);

            using var claimsResponse =
                await _client.SendAsync(
                    claimsRequest);

            Assert.Equal(
                HttpStatusCode.OK,
                claimsResponse.StatusCode);

            var claims =
                await claimsResponse.Content
                    .ReadFromJsonAsync<AuthenticationClaimsResponse>(
                        JsonOptions);

            Assert.NotNull(
                claims);

            Assert.True(
                claims!.IsAuthenticated);

            Assert.Equal(
                testUser.UserAccountId.ToString("D"),
                claims.UserAccountId);

            Assert.Equal(
                testUser.EmployeeId.ToString("D"),
                claims.EmployeeId);

            Assert.Equal(
                testUser.Email,
                claims.Email);

            Assert.Equal(
                EmployeeRole.Employee.ToString(),
                claims.Role);
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
        string password)
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
            $"integration.bearer.{Guid.NewGuid():N}@example.com";

        var employee =
            new Employee
            {
                Id = employeeId,
                FirstName = "Integration",
                LastName = "BearerUser",
                Email = email,
                DepartmentId = departmentId,
                ManagerId = null,
                Role = EmployeeRole.Employee,
                IsActive = true,
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
