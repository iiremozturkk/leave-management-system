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

public sealed class UpdateEmployeeEndpointTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
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

            AssertEmployeeStateUnchanged(
                employeeBeforeUpdate!,
                employeeAfterUpdate!);
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

            AssertEmployeeStateUnchanged(
                employeeBeforeUpdate!,
                employeeAfterUpdate!);
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

            AssertEmployeeStateUnchanged(
                employeeBeforeUpdate!,
                employeeAfterUpdate!);
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

            AssertEmployeeStateUnchanged(
                targetEmployeeBeforeUpdate!,
                targetEmployeeAfterUpdate!);
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

            AssertEmployeeStateUnchanged(
                employeeBeforeUpdate!,
                employeeAfterUpdate!);
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

    private static void AssertEmployeeStateUnchanged(
        EmployeeResponse expected,
        EmployeeResponse actual)
    {
        Assert.Equal(
            expected.FirstName,
            actual.FirstName);

        Assert.Equal(
            expected.LastName,
            actual.LastName);

        Assert.Equal(
            expected.Email,
            actual.Email);

        Assert.Equal(
            expected.DepartmentId,
            actual.DepartmentId);

        Assert.Equal(
            expected.ManagerId,
            actual.ManagerId);

        Assert.Equal(
            expected.Role,
            actual.Role);

        Assert.Equal(
            expected.IsActive,
            actual.IsActive);
    }
}
