using System.Net;
using System.Net.Http.Json;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.Employees;

public sealed class EmployeeCrudEndpointTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task EmployeeCrudFlow_WorksThroughApi()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

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

            Assert.NotNull(
                createdEmployee);

            employeeId =
                createdEmployee!.Id;

            var location =
                createResponse.Headers.Location;

            Assert.NotNull(
                location);

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

            Assert.NotNull(
                loadedEmployee);

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

            Assert.NotNull(
                employees);

            Assert.Contains(
                employees!,
                employee =>
                    employee.Id == employeeId);

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

            Assert.NotNull(
                updatedEmployee);

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
}
