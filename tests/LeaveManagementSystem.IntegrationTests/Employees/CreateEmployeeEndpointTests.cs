using System.Net;
using System.Net.Http.Json;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.Employees;

public sealed class CreateEmployeeEndpointTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateEmployee_InvalidEmail_ReturnsValidationProblemDetails()
    {
        var request = new
        {
            firstName = "Irem",
            lastName = "Ozturk",
            email = "invalid-email",
            departmentId = Guid.NewGuid(),
            managerId = (Guid?)null,
            role = EmployeeRole.Employee
        };

        using var response =
            await _client.PostAsJsonAsync(
                "/api/employees",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers
                .ContentType?.MediaType);

        var problem =
            await response.Content
                .ReadFromJsonAsync<
                    ValidationProblemDetailsResponse>(
                        JsonOptions);

        Assert.NotNull(
            problem);

        Assert.Equal(
            400,
            problem!.Status);

        Assert.Equal(
            "One or more validation errors occurred.",
            problem.Title);

        Assert.Equal(
            "/api/employees",
            problem.Instance);

        Assert.True(
            problem.Errors.TryGetValue(
                "Email",
                out var messages));

        Assert.NotNull(
            messages);

        Assert.Contains(
            "Email must be a valid email address.",
            messages!);

        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.TraceId));
    }

    [Fact]
    public async Task CreateEmployee_DepartmentDoesNotExist_ReturnsBusinessRuleProblemDetails()
    {
        await EnsureDatabaseReadyAsync();

        var request = new
        {
            firstName = "Irem",
            lastName = "Ozturk",
            email =
                $"missing.department.{Guid.NewGuid():N}@example.com",
            departmentId = Guid.NewGuid(),
            managerId = (Guid?)null,
            role = EmployeeRole.Employee
        };

        using var response =
            await _client.PostAsJsonAsync(
                "/api/employees",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers
                .ContentType?.MediaType);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetailsResponse>(
                    JsonOptions);

        Assert.NotNull(
            problem);

        AssertBusinessRuleProblemDetails(
            problem!,
            "Department does not exist.",
            "/api/employees");
    }

    [Fact]
    public async Task CreateEmployee_ManagerDoesNotExist_ReturnsBusinessRuleProblemDetails()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? unexpectedEmployeeId = null;

        try
        {
            var request = new
            {
                firstName = "Irem",
                lastName = "Ozturk",
                email =
                    $"missing.manager.{Guid.NewGuid():N}@example.com",
                departmentId,
                managerId = Guid.NewGuid(),
                role = EmployeeRole.Employee
            };

            using var response =
                await _client.PostAsJsonAsync(
                    "/api/employees",
                    request);

            if (response.StatusCode ==
                HttpStatusCode.Created)
            {
                var unexpectedEmployee =
                    await response.Content
                        .ReadFromJsonAsync<EmployeeResponse>(
                            JsonOptions);

                unexpectedEmployeeId =
                    unexpectedEmployee?.Id;
            }

            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);

            Assert.Equal(
                "application/problem+json",
                response.Content.Headers
                    .ContentType?.MediaType);

            var problem =
                await response.Content
                    .ReadFromJsonAsync<ProblemDetailsResponse>(
                        JsonOptions);

            Assert.NotNull(
                problem);

            AssertBusinessRuleProblemDetails(
                problem!,
                "Manager does not exist or is not active.",
                "/api/employees");
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: unexpectedEmployeeId,
                departmentId: departmentId);
        }
    }

    [Fact]
    public async Task CreateEmployee_NormalizedEmailAlreadyExists_ReturnsBusinessRuleProblemDetails()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;
        Guid? unexpectedDuplicateEmployeeId = null;

        try
        {
            var email =
                $"duplicate.employee.{Guid.NewGuid():N}@example.com";

            var firstRequest = new
            {
                firstName = "Original",
                lastName = "Employee",
                email,
                departmentId,
                managerId = (Guid?)null,
                role = EmployeeRole.Employee
            };

            using var firstResponse =
                await _client.PostAsJsonAsync(
                    "/api/employees",
                    firstRequest);

            Assert.Equal(
                HttpStatusCode.Created,
                firstResponse.StatusCode);

            var createdEmployee =
                await firstResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                createdEmployee);

            employeeId =
                createdEmployee!.Id;

            var duplicateRequest = new
            {
                firstName = "Duplicate",
                lastName = "Employee",
                email = email.ToUpperInvariant(),
                departmentId,
                managerId = (Guid?)null,
                role = EmployeeRole.Employee
            };

            using var duplicateResponse =
                await _client.PostAsJsonAsync(
                    "/api/employees",
                    duplicateRequest);

            if (duplicateResponse.StatusCode ==
                HttpStatusCode.Created)
            {
                var unexpectedDuplicateEmployee =
                    await duplicateResponse.Content
                        .ReadFromJsonAsync<EmployeeResponse>(
                            JsonOptions);

                unexpectedDuplicateEmployeeId =
                    unexpectedDuplicateEmployee?.Id;
            }

            Assert.Equal(
                HttpStatusCode.BadRequest,
                duplicateResponse.StatusCode);

            Assert.Equal(
                "application/problem+json",
                duplicateResponse.Content.Headers
                    .ContentType?.MediaType);

            var problem =
                await duplicateResponse.Content
                    .ReadFromJsonAsync<ProblemDetailsResponse>(
                        JsonOptions);

            Assert.NotNull(
                problem);

            AssertBusinessRuleProblemDetails(
                problem!,
                "Email is already used by another employee.",
                "/api/employees");

            using var scope =
                _factory.Services.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            var employeeCount =
                await dbContext.Employees.CountAsync(
                    employee =>
                        employee.DepartmentId ==
                        departmentId);

            Assert.Equal(
                1,
                employeeCount);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: unexpectedDuplicateEmployeeId,
                departmentId: null);

            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }
}
