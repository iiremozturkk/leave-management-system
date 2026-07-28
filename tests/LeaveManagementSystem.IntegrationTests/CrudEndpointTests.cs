using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests;

public sealed class CrudEndpointTests
    : IClassFixture<TestWebApplicationFactory>
{
    private static readonly Guid AnnualLeaveTypeId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CrudEndpointTests(
        TestWebApplicationFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
    }

    [Fact]
    public async Task EmployeeCrudFlow_WorksThroughApi()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId = await CreateDepartmentAsync();
        Guid? employeeId = null;

        try
        {
            var listBeforeCreateResponse =
                await _client.GetAsync(
                    "/api/employees");

            Assert.Equal(
                HttpStatusCode.OK,
                listBeforeCreateResponse.StatusCode);

            var suffix =
                Guid.NewGuid().ToString("N");

            var email =
                $"integration.employee.{suffix}@example.com";

            var createRequest = new
            {
                firstName = "  Integration  ",
                lastName = "  Employee  ",
                email = email.ToUpperInvariant(),
                departmentId,
                managerId = (Guid?)null,
                role = EmployeeRole.Employee
            };

            var createResponse =
                await _client.PostAsJsonAsync(
                    "/api/employees",
                    createRequest);

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var createdEmployee =
                await createResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(createdEmployee);

            employeeId = createdEmployee!.Id;

            var location =
                createResponse.Headers.Location;

            Assert.NotNull(location);

            var locationPath =
                location!.IsAbsoluteUri
                    ? location.AbsolutePath
                    : location.OriginalString;

            Assert.Equal(
                $"/api/employees/{createdEmployee.Id}",
                locationPath);

            Assert.Equal(
                "Integration",
                createdEmployee.FirstName);

            Assert.Equal(
                "Employee",
                createdEmployee.LastName);

            Assert.Equal(
                email,
                createdEmployee.Email);

            Assert.Equal(
                departmentId,
                createdEmployee.DepartmentId);

            Assert.Equal(
                EmployeeRole.Employee,
                createdEmployee.Role);

            Assert.True(
                createdEmployee.IsActive);

            var getByIdResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getByIdResponse.StatusCode);

            var loadedEmployee =
                await getByIdResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(loadedEmployee);

            Assert.Equal(
                employeeId,
                loadedEmployee!.Id);

            var listAfterCreateResponse =
                await _client.GetAsync(
                    "/api/employees");

            Assert.Equal(
                HttpStatusCode.OK,
                listAfterCreateResponse.StatusCode);

            var employees =
                await listAfterCreateResponse.Content
                    .ReadFromJsonAsync<List<EmployeeResponse>>(
                        JsonOptions);

            Assert.NotNull(employees);

            Assert.Contains(
                employees!,
                employee => employee.Id == employeeId);

            var updatedEmail =
                $"integration.employee.updated.{suffix}@example.com";

            var updateRequest = new
            {
                firstName = "  IntegrationUpdated  ",
                lastName = "  EmployeeUpdated  ",
                email = updatedEmail.ToUpperInvariant(),
                departmentId,
                managerId = (Guid?)null,
                role = EmployeeRole.Manager,
                isActive = false
            };

            var updateResponse =
                await _client.PutAsJsonAsync(
                    $"/api/employees/{employeeId}",
                    updateRequest);

            Assert.Equal(
                HttpStatusCode.OK,
                updateResponse.StatusCode);

            var updatedEmployee =
                await updateResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(updatedEmployee);

            Assert.Equal(
                "IntegrationUpdated",
                updatedEmployee!.FirstName);

            Assert.Equal(
                "EmployeeUpdated",
                updatedEmployee.LastName);

            Assert.Equal(
                updatedEmail,
                updatedEmployee.Email);

            Assert.Equal(
                departmentId,
                updatedEmployee.DepartmentId);

            Assert.Null(
                updatedEmployee.ManagerId);

            Assert.Equal(
                EmployeeRole.Manager,
                updatedEmployee.Role);

            Assert.False(
                updatedEmployee.IsActive);

            var getUpdatedEmployeeResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getUpdatedEmployeeResponse.StatusCode);

            var persistedEmployee =
                await getUpdatedEmployeeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                persistedEmployee);

            Assert.Equal(
                employeeId,
                persistedEmployee!.Id);

            Assert.Equal(
                "IntegrationUpdated",
                persistedEmployee.FirstName);

            Assert.Equal(
                "EmployeeUpdated",
                persistedEmployee.LastName);

            Assert.Equal(
                updatedEmail,
                persistedEmployee.Email);

            Assert.Equal(
                departmentId,
                persistedEmployee.DepartmentId);

            Assert.Null(
                persistedEmployee.ManagerId);

            Assert.Equal(
                EmployeeRole.Manager,
                persistedEmployee.Role);

            Assert.False(
                persistedEmployee.IsActive);

            var deleteResponse =
                await _client.DeleteAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.NoContent,
                deleteResponse.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }

    [Fact]
    public async Task UpdateEmployee_InvalidEmail_ReturnsValidationProblemDetails()
    {
        var employeeId = Guid.NewGuid();

        var request = new
        {
            firstName = "Irem",
            lastName = "Ozturk",
            email = "invalid-email",
            departmentId = Guid.NewGuid(),
            managerId = (Guid?)null,
            role = EmployeeRole.Employee,
            isActive = true
        };

        using var response =
            await _client.PutAsJsonAsync(
                $"/api/employees/{employeeId}",
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

        Assert.NotNull(problem);

        Assert.Equal(
            400,
            problem!.Status);

        Assert.Equal(
            "One or more validation errors occurred.",
            problem.Title);

        Assert.Equal(
            $"/api/employees/{employeeId}",
            problem.Instance);

        Assert.True(
            problem.Errors.TryGetValue(
                "Email",
                out var messages));

        Assert.NotNull(messages);

        Assert.Contains(
            "Email must be a valid email address.",
            messages!);

        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.TraceId));
    }

    [Fact]
    public async Task UpdateEmployee_EmployeeDoesNotExist_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        var employeeId = Guid.NewGuid();

        var request = new
        {
            firstName = "Irem",
            lastName = "Ozturk",
            email =
                $"missing.employee.{Guid.NewGuid():N}@example.com",
            departmentId = Guid.NewGuid(),
            managerId = (Guid?)null,
            role = EmployeeRole.Employee,
            isActive = true
        };

        using var response =
            await _client.PutAsJsonAsync(
                $"/api/employees/{employeeId}",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_DepartmentDoesNotExist_ReturnsBusinessRuleProblemDetails()
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

            using var getBeforeResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getBeforeResponse.StatusCode);

            var employeeBeforeUpdate =
                await getBeforeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeBeforeUpdate);

            var request = new
            {
                firstName = "Updated",
                lastName = "Employee",
                email =
                    $"missing.department.update.{Guid.NewGuid():N}@example.com",
                departmentId = Guid.NewGuid(),
                managerId = (Guid?)null,
                role = EmployeeRole.Manager,
                isActive = false
            };

            using var response =
                await _client.PutAsJsonAsync(
                    $"/api/employees/{employeeId}",
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

            Assert.NotNull(problem);

            Assert.Equal(
                400,
                problem!.Status);

            Assert.Equal(
                "A business rule was violated.",
                problem.Title);

            Assert.Equal(
                "Department does not exist.",
                problem.Detail);

            Assert.Equal(
                $"/api/employees/{employeeId}",
                problem.Instance);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    problem.TraceId));

            using var getAfterResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getAfterResponse.StatusCode);

            var employeeAfterUpdate =
                await getAfterResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeAfterUpdate);

            Assert.Equal(
                employeeBeforeUpdate!.FirstName,
                employeeAfterUpdate!.FirstName);

            Assert.Equal(
                employeeBeforeUpdate.LastName,
                employeeAfterUpdate.LastName);

            Assert.Equal(
                employeeBeforeUpdate.Email,
                employeeAfterUpdate.Email);

            Assert.Equal(
                employeeBeforeUpdate.DepartmentId,
                employeeAfterUpdate.DepartmentId);

            Assert.Equal(
                employeeBeforeUpdate.ManagerId,
                employeeAfterUpdate.ManagerId);

            Assert.Equal(
                employeeBeforeUpdate.Role,
                employeeAfterUpdate.Role);

            Assert.Equal(
                employeeBeforeUpdate.IsActive,
                employeeAfterUpdate.IsActive);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }

    [Fact]
    public async Task UpdateEmployee_ManagerDoesNotExist_ReturnsBusinessRuleProblemDetails()
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

            using var getBeforeResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getBeforeResponse.StatusCode);

            var employeeBeforeUpdate =
                await getBeforeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeBeforeUpdate);

            var request = new
            {
                firstName = "Updated",
                lastName = "Employee",
                email =
                    $"missing.manager.update.{Guid.NewGuid():N}@example.com",
                departmentId,
                managerId = Guid.NewGuid(),
                role = EmployeeRole.Manager,
                isActive = false
            };

            using var response =
                await _client.PutAsJsonAsync(
                    $"/api/employees/{employeeId}",
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

            Assert.NotNull(problem);

            Assert.Equal(
                400,
                problem!.Status);

            Assert.Equal(
                "A business rule was violated.",
                problem.Title);

            Assert.Equal(
                "Manager does not exist or is not active.",
                problem.Detail);

            Assert.Equal(
                $"/api/employees/{employeeId}",
                problem.Instance);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    problem.TraceId));

            using var getAfterResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getAfterResponse.StatusCode);

            var employeeAfterUpdate =
                await getAfterResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeAfterUpdate);

            Assert.Equal(
                employeeBeforeUpdate!.FirstName,
                employeeAfterUpdate!.FirstName);

            Assert.Equal(
                employeeBeforeUpdate.LastName,
                employeeAfterUpdate.LastName);

            Assert.Equal(
                employeeBeforeUpdate.Email,
                employeeAfterUpdate.Email);

            Assert.Equal(
                employeeBeforeUpdate.DepartmentId,
                employeeAfterUpdate.DepartmentId);

            Assert.Equal(
                employeeBeforeUpdate.ManagerId,
                employeeAfterUpdate.ManagerId);

            Assert.Equal(
                employeeBeforeUpdate.Role,
                employeeAfterUpdate.Role);

            Assert.Equal(
                employeeBeforeUpdate.IsActive,
                employeeAfterUpdate.IsActive);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }

    [Fact]
    public async Task UpdateEmployee_EmployeeIsOwnManager_ReturnsBusinessRuleProblemDetails()
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

            using var getBeforeResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getBeforeResponse.StatusCode);

            var employeeBeforeUpdate =
                await getBeforeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeBeforeUpdate);

            var request = new
            {
                firstName = "Updated",
                lastName = "Employee",
                email =
                    $"self.manager.update.{Guid.NewGuid():N}@example.com",
                departmentId,
                managerId = employeeId,
                role = EmployeeRole.Manager,
                isActive = false
            };

            using var response =
                await _client.PutAsJsonAsync(
                    $"/api/employees/{employeeId}",
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

            Assert.NotNull(problem);

            Assert.Equal(
                400,
                problem!.Status);

            Assert.Equal(
                "A business rule was violated.",
                problem.Title);

            Assert.Equal(
                "An employee cannot be their own manager.",
                problem.Detail);

            Assert.Equal(
                $"/api/employees/{employeeId}",
                problem.Instance);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    problem.TraceId));

            using var getAfterResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getAfterResponse.StatusCode);

            var employeeAfterUpdate =
                await getAfterResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeAfterUpdate);

            Assert.Equal(
                employeeBeforeUpdate!.FirstName,
                employeeAfterUpdate!.FirstName);

            Assert.Equal(
                employeeBeforeUpdate.LastName,
                employeeAfterUpdate.LastName);

            Assert.Equal(
                employeeBeforeUpdate.Email,
                employeeAfterUpdate.Email);

            Assert.Equal(
                employeeBeforeUpdate.DepartmentId,
                employeeAfterUpdate.DepartmentId);

            Assert.Equal(
                employeeBeforeUpdate.ManagerId,
                employeeAfterUpdate.ManagerId);

            Assert.Equal(
                employeeBeforeUpdate.Role,
                employeeAfterUpdate.Role);

            Assert.Equal(
                employeeBeforeUpdate.IsActive,
                employeeAfterUpdate.IsActive);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }

    [Fact]
    public async Task UpdateEmployee_NormalizedEmailAlreadyExists_ReturnsBusinessRuleProblemDetails()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? targetEmployeeId = null;
        Guid? existingEmployeeId = null;

        try
        {
            targetEmployeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            using var getBeforeResponse =
                await _client.GetAsync(
                    $"/api/employees/{targetEmployeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getBeforeResponse.StatusCode);

            var targetEmployeeBeforeUpdate =
                await getBeforeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                targetEmployeeBeforeUpdate);

            var existingEmail =
                $"existing.update.email.{Guid.NewGuid():N}@example.com";

            var createExistingEmployeeRequest = new
            {
                firstName = "Existing",
                lastName = "Employee",
                email = existingEmail,
                departmentId,
                managerId = (Guid?)null,
                role = EmployeeRole.Employee
            };

            using var createExistingEmployeeResponse =
                await _client.PostAsJsonAsync(
                    "/api/employees",
                    createExistingEmployeeRequest);

            Assert.Equal(
                HttpStatusCode.Created,
                createExistingEmployeeResponse.StatusCode);

            var existingEmployee =
                await createExistingEmployeeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                existingEmployee);

            existingEmployeeId =
                existingEmployee!.Id;

            var updateRequest = new
            {
                firstName = "Updated",
                lastName = "Employee",
                email = existingEmail.ToUpperInvariant(),
                departmentId,
                managerId = (Guid?)null,
                role = EmployeeRole.Manager,
                isActive = false
            };

            using var updateResponse =
                await _client.PutAsJsonAsync(
                    $"/api/employees/{targetEmployeeId}",
                    updateRequest);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                updateResponse.StatusCode);

            Assert.Equal(
                "application/problem+json",
                updateResponse.Content.Headers
                    .ContentType?.MediaType);

            var problem =
                await updateResponse.Content
                    .ReadFromJsonAsync<ProblemDetailsResponse>(
                        JsonOptions);

            Assert.NotNull(problem);

            Assert.Equal(
                400,
                problem!.Status);

            Assert.Equal(
                "A business rule was violated.",
                problem.Title);

            Assert.Equal(
                "Email is already used by another employee.",
                problem.Detail);

            Assert.Equal(
                $"/api/employees/{targetEmployeeId}",
                problem.Instance);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    problem.TraceId));

            using var getAfterResponse =
                await _client.GetAsync(
                    $"/api/employees/{targetEmployeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getAfterResponse.StatusCode);

            var targetEmployeeAfterUpdate =
                await getAfterResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                targetEmployeeAfterUpdate);

            Assert.Equal(
                targetEmployeeBeforeUpdate!.FirstName,
                targetEmployeeAfterUpdate!.FirstName);

            Assert.Equal(
                targetEmployeeBeforeUpdate.LastName,
                targetEmployeeAfterUpdate.LastName);

            Assert.Equal(
                targetEmployeeBeforeUpdate.Email,
                targetEmployeeAfterUpdate.Email);

            Assert.Equal(
                targetEmployeeBeforeUpdate.DepartmentId,
                targetEmployeeAfterUpdate.DepartmentId);

            Assert.Equal(
                targetEmployeeBeforeUpdate.ManagerId,
                targetEmployeeAfterUpdate.ManagerId);

            Assert.Equal(
                targetEmployeeBeforeUpdate.Role,
                targetEmployeeAfterUpdate.Role);

            Assert.Equal(
                targetEmployeeBeforeUpdate.IsActive,
                targetEmployeeAfterUpdate.IsActive);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: existingEmployeeId,
                departmentId: null);

            await CleanupAsync(
                leaveRequestId: null,
                employeeId: targetEmployeeId,
                departmentId: departmentId);
        }
    }

    [Fact]
    public async Task UpdateEmployee_CurrentNormalizedEmail_ReturnsOkAndPersists()
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

            using var getBeforeResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getBeforeResponse.StatusCode);

            var employeeBeforeUpdate =
                await getBeforeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeBeforeUpdate);

            var updateRequest = new
            {
                firstName = "  SameEmail  ",
                lastName = "  EmployeeUpdated  ",
                email =
                    employeeBeforeUpdate!.Email.ToUpperInvariant(),
                departmentId,
                managerId = (Guid?)null,
                role = EmployeeRole.Manager,
                isActive = false
            };

            using var updateResponse =
                await _client.PutAsJsonAsync(
                    $"/api/employees/{employeeId}",
                    updateRequest);

            Assert.Equal(
                HttpStatusCode.OK,
                updateResponse.StatusCode);

            var updatedEmployee =
                await updateResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                updatedEmployee);

            Assert.Equal(
                employeeId,
                updatedEmployee!.Id);

            Assert.Equal(
                "SameEmail",
                updatedEmployee.FirstName);

            Assert.Equal(
                "EmployeeUpdated",
                updatedEmployee.LastName);

            Assert.Equal(
                employeeBeforeUpdate.Email,
                updatedEmployee.Email);

            Assert.Equal(
                departmentId,
                updatedEmployee.DepartmentId);

            Assert.Null(
                updatedEmployee.ManagerId);

            Assert.Equal(
                EmployeeRole.Manager,
                updatedEmployee.Role);

            Assert.False(
                updatedEmployee.IsActive);

            using var getAfterResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getAfterResponse.StatusCode);

            var persistedEmployee =
                await getAfterResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                persistedEmployee);

            Assert.Equal(
                employeeId,
                persistedEmployee!.Id);

            Assert.Equal(
                "SameEmail",
                persistedEmployee.FirstName);

            Assert.Equal(
                "EmployeeUpdated",
                persistedEmployee.LastName);

            Assert.Equal(
                employeeBeforeUpdate.Email,
                persistedEmployee.Email);

            Assert.Equal(
                departmentId,
                persistedEmployee.DepartmentId);

            Assert.Null(
                persistedEmployee.ManagerId);

            Assert.Equal(
                EmployeeRole.Manager,
                persistedEmployee.Role);

            Assert.False(
                persistedEmployee.IsActive);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }

    [Fact]
    public async Task UpdateEmployee_ManagerCanBeAssignedAndRemoved_PersistsBothChanges()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? managerId = null;
        Guid? employeeId = null;

        try
        {
            // Create an active employee to be assigned as the manager.
            managerId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            var updatedEmail =
                $"manager.assignment.{Guid.NewGuid():N}@example.com";

            var assignManagerRequest = new
            {
                firstName = "Managed",
                lastName = "Employee",
                email = updatedEmail,
                departmentId,
                managerId,
                role = EmployeeRole.Employee,
                isActive = true
            };

            using var assignManagerResponse =
                await _client.PutAsJsonAsync(
                    $"/api/employees/{employeeId}",
                    assignManagerRequest);

            Assert.Equal(
                HttpStatusCode.OK,
                assignManagerResponse.StatusCode);

            var employeeWithManager =
                await assignManagerResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeWithManager);

            Assert.Equal(
                employeeId,
                employeeWithManager!.Id);

            Assert.Equal(
                "Managed",
                employeeWithManager.FirstName);

            Assert.Equal(
                updatedEmail,
                employeeWithManager.Email);

            Assert.Equal(
                departmentId,
                employeeWithManager.DepartmentId);

            Assert.Equal(
                managerId,
                employeeWithManager.ManagerId);

            Assert.True(
                employeeWithManager.IsActive);

            using var getAssignedEmployeeResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getAssignedEmployeeResponse.StatusCode);

            var persistedEmployeeWithManager =
                await getAssignedEmployeeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                persistedEmployeeWithManager);

            Assert.Equal(
                employeeId,
                persistedEmployeeWithManager!.Id);

            Assert.Equal(
                "Managed",
                persistedEmployeeWithManager.FirstName);

            Assert.Equal(
                updatedEmail,
                persistedEmployeeWithManager.Email);

            Assert.Equal(
                departmentId,
                persistedEmployeeWithManager.DepartmentId);

            Assert.Equal(
                managerId,
                persistedEmployeeWithManager.ManagerId);

            Assert.True(
                persistedEmployeeWithManager.IsActive);

            var removeManagerRequest = new
            {
                firstName = "Unmanaged",
                lastName = "Employee",
                email = updatedEmail,
                departmentId,
                managerId = (Guid?)null,
                role = EmployeeRole.Employee,
                isActive = true
            };

            using var removeManagerResponse =
                await _client.PutAsJsonAsync(
                    $"/api/employees/{employeeId}",
                    removeManagerRequest);

            Assert.Equal(
                HttpStatusCode.OK,
                removeManagerResponse.StatusCode);

            var employeeWithoutManager =
                await removeManagerResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeWithoutManager);

            Assert.Equal(
                employeeId,
                employeeWithoutManager!.Id);

            Assert.Equal(
                "Unmanaged",
                employeeWithoutManager.FirstName);

            Assert.Equal(
                updatedEmail,
                employeeWithoutManager.Email);

            Assert.Equal(
                departmentId,
                employeeWithoutManager.DepartmentId);

            Assert.Null(
                employeeWithoutManager.ManagerId);

            Assert.True(
                employeeWithoutManager.IsActive);

            using var getRemovedManagerResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getRemovedManagerResponse.StatusCode);

            var persistedEmployeeWithoutManager =
                await getRemovedManagerResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                persistedEmployeeWithoutManager);

            Assert.Equal(
                employeeId,
                persistedEmployeeWithoutManager!.Id);

            Assert.Equal(
                "Unmanaged",
                persistedEmployeeWithoutManager.FirstName);

            Assert.Equal(
                updatedEmail,
                persistedEmployeeWithoutManager.Email);

            Assert.Equal(
                departmentId,
                persistedEmployeeWithoutManager.DepartmentId);

            Assert.Null(
                persistedEmployeeWithoutManager.ManagerId);

            Assert.True(
                persistedEmployeeWithoutManager.IsActive);
        }
        finally
        {
            // Delete the potentially dependent employee first.
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeId,
                departmentId: null);

            // Then delete the employee used as the manager and the department.
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: managerId,
                departmentId: departmentId);
        }
    }

    [Fact]
    public async Task UpdateEmployee_InactiveManager_ReturnsBusinessRuleProblemDetails()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? managerId = null;
        Guid? employeeId = null;

        try
        {
            managerId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            using var getBeforeResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getBeforeResponse.StatusCode);

            var employeeBeforeUpdate =
                await getBeforeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeBeforeUpdate);

            using var deleteManagerResponse =
                await _client.DeleteAsync(
                    $"/api/employees/{managerId}");

            Assert.Equal(
                HttpStatusCode.NoContent,
                deleteManagerResponse.StatusCode);

            Assert.NotNull(
                managerId);

            var inactiveManagerId =
                managerId.Value;

            using (var scope =
                   _factory.Services.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                var inactiveManager =
                    await dbContext.Employees
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .SingleAsync(
                            employee =>
                                employee.Id == inactiveManagerId);

                Assert.False(
                    inactiveManager.IsActive);
            }

            var updateRequest = new
            {
                firstName = "Updated",
                lastName = "Employee",
                email =
                    $"inactive.manager.update.{Guid.NewGuid():N}@example.com",
                departmentId,
                managerId = inactiveManagerId,
                role = EmployeeRole.Manager,
                isActive = false
            };

            using var updateResponse =
                await _client.PutAsJsonAsync(
                    $"/api/employees/{employeeId}",
                    updateRequest);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                updateResponse.StatusCode);

            Assert.Equal(
                "application/problem+json",
                updateResponse.Content.Headers
                    .ContentType?.MediaType);

            var problem =
                await updateResponse.Content
                    .ReadFromJsonAsync<ProblemDetailsResponse>(
                        JsonOptions);

            Assert.NotNull(
                problem);

            Assert.Equal(
                400,
                problem!.Status);

            Assert.Equal(
                "A business rule was violated.",
                problem.Title);

            Assert.Equal(
                "Manager does not exist or is not active.",
                problem.Detail);

            Assert.Equal(
                $"/api/employees/{employeeId}",
                problem.Instance);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    problem.TraceId));

            using var getAfterResponse =
                await _client.GetAsync(
                    $"/api/employees/{employeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getAfterResponse.StatusCode);

            var employeeAfterUpdate =
                await getAfterResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employeeAfterUpdate);

            Assert.Equal(
                employeeBeforeUpdate!.FirstName,
                employeeAfterUpdate!.FirstName);

            Assert.Equal(
                employeeBeforeUpdate.LastName,
                employeeAfterUpdate.LastName);

            Assert.Equal(
                employeeBeforeUpdate.Email,
                employeeAfterUpdate.Email);

            Assert.Equal(
                employeeBeforeUpdate.DepartmentId,
                employeeAfterUpdate.DepartmentId);

            Assert.Equal(
                employeeBeforeUpdate.ManagerId,
                employeeAfterUpdate.ManagerId);

            Assert.Equal(
                employeeBeforeUpdate.Role,
                employeeAfterUpdate.Role);

            Assert.Equal(
                employeeBeforeUpdate.IsActive,
                employeeAfterUpdate.IsActive);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeId,
                departmentId: null);

            await CleanupAsync(
                leaveRequestId: null,
                employeeId: managerId,
                departmentId: departmentId);
        }
    }

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

        Assert.NotNull(problem);

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

        Assert.NotNull(messages);

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

        Assert.NotNull(problem);

        Assert.Equal(
            400,
            problem!.Status);

        Assert.Equal(
            "A business rule was violated.",
            problem.Title);

        Assert.Equal(
            "Department does not exist.",
            problem.Detail);

        Assert.Equal(
            "/api/employees",
            problem.Instance);

        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.TraceId));
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

            Assert.NotNull(problem);

            Assert.Equal(
                400,
                problem!.Status);

            Assert.Equal(
                "A business rule was violated.",
                problem.Title);

            Assert.Equal(
                "Manager does not exist or is not active.",
                problem.Detail);

            Assert.Equal(
                "/api/employees",
                problem.Instance);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    problem.TraceId));
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

            Assert.NotNull(createdEmployee);

            employeeId = createdEmployee!.Id;

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

            Assert.NotNull(problem);

            Assert.Equal(
                400,
                problem!.Status);

            Assert.Equal(
                "A business rule was violated.",
                problem.Title);

            Assert.Equal(
                "Email is already used by another employee.",
                problem.Detail);

            Assert.Equal(
                "/api/employees",
                problem.Instance);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    problem.TraceId));

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

    [Fact]
    public async Task LeaveRequestCrudFlow_WorksThroughApi()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;
        Guid? leaveRequestId = null;

        try
        {
            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            var listBeforeCreateResponse =
                await _client.GetAsync(
                    "/api/leave-requests");

            Assert.Equal(
                HttpStatusCode.OK,
                listBeforeCreateResponse.StatusCode);

            var createRequest = new
            {
                employeeId,
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-07-15",
                endDate = "2026-07-17",
                reason =
                    "Integration test leave request."
            };

            var createResponse =
                await _client.PostAsJsonAsync(
                    "/api/leave-requests",
                    createRequest);

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var createdLeaveRequest =
                await createResponse.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(createdLeaveRequest);

            leaveRequestId =
                createdLeaveRequest!.Id;

            Assert.Equal(
                employeeId,
                createdLeaveRequest.EmployeeId);

            Assert.Equal(
                AnnualLeaveTypeId,
                createdLeaveRequest.LeaveTypeId);

            Assert.Equal(
                3,
                createdLeaveRequest.RequestedDays);

            Assert.Equal(
                LeaveRequestStatus.Pending,
                createdLeaveRequest.Status);

            Assert.Equal(
                "Integration test leave request.",
                createdLeaveRequest.Reason);

            var getByIdResponse =
                await _client.GetAsync(
                    $"/api/leave-requests/{leaveRequestId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getByIdResponse.StatusCode);

            var loadedLeaveRequest =
                await getByIdResponse.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(loadedLeaveRequest);

            Assert.Equal(
                leaveRequestId,
                loadedLeaveRequest!.Id);

            var listAfterCreateResponse =
                await _client.GetAsync(
                    "/api/leave-requests");

            Assert.Equal(
                HttpStatusCode.OK,
                listAfterCreateResponse.StatusCode);

            var leaveRequests =
                await listAfterCreateResponse.Content
                    .ReadFromJsonAsync<List<LeaveRequestResponse>>(
                        JsonOptions);

            Assert.NotNull(leaveRequests);

            Assert.Contains(
                leaveRequests!,
                leaveRequest =>
                    leaveRequest.Id == leaveRequestId);

            var updateRequest = new
            {
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-07-16",
                endDate = "2026-07-18",
                reason =
                    "Updated integration test leave request."
            };

            var updateResponse =
                await _client.PutAsJsonAsync(
                    $"/api/leave-requests/{leaveRequestId}",
                    updateRequest);

            Assert.Equal(
                HttpStatusCode.OK,
                updateResponse.StatusCode);

            var updatedLeaveRequest =
                await updateResponse.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(updatedLeaveRequest);

            Assert.Equal(
                3,
                updatedLeaveRequest!.RequestedDays);

            Assert.Equal(
                LeaveRequestStatus.Pending,
                updatedLeaveRequest.Status);

            Assert.Equal(
                "Updated integration test leave request.",
                updatedLeaveRequest.Reason);

            var deleteResponse =
                await _client.DeleteAsync(
                    $"/api/leave-requests/{leaveRequestId}");

            Assert.Equal(
                HttpStatusCode.NoContent,
                deleteResponse.StatusCode);

            leaveRequestId = null;
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: leaveRequestId,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }

    private async Task EnsureDatabaseReadyAsync()
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
                    leaveType.Id == AnnualLeaveTypeId);

        Assert.True(
            annualLeaveExists,
            "The default Annual Leave type should exist in the test database.");
    }

    private async Task<Guid> CreateDepartmentAsync()
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

    private async Task<Guid> CreateEmployeeViaApiAsync(
        Guid departmentId)
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
            role = EmployeeRole.Employee
        };

        var createResponse =
            await _client.PostAsJsonAsync(
                "/api/employees",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdEmployee =
            await createResponse.Content
                .ReadFromJsonAsync<EmployeeResponse>(
                    JsonOptions);

        Assert.NotNull(createdEmployee);

        return createdEmployee!.Id;
    }

    private async Task CleanupAsync(
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

    private sealed record EmployeeResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        Guid DepartmentId,
        Guid? ManagerId,
        EmployeeRole Role,
        bool IsActive);

    private sealed record LeaveRequestResponse(
        Guid Id,
        Guid EmployeeId,
        Guid LeaveTypeId,
        int RequestedDays,
        LeaveRequestStatus Status,
        string Reason);

    private sealed record ValidationProblemDetailsResponse(
        string Title,
        int Status,
        string? Instance,
        Dictionary<string, string[]> Errors,
        string TraceId);

    private sealed record ProblemDetailsResponse(
        string Title,
        string Detail,
        int Status,
        string? Instance,
        string TraceId);
}
