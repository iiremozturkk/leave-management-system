using System.Net;
using System.Net.Http.Json;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.Employees;

public sealed class DeleteEmployeeEndpointTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task DeleteEmployee_EmployeeDoesNotExist_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        var employeeId =
            Guid.NewGuid();

        using var response =
            await _client.DeleteAsync(
                $"/api/employees/{employeeId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task DeleteEmployee_EmployeeExists_SoftDeletesAndReturnsNoContent()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            var createdEmployeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            employeeId =
                createdEmployeeId;

            using var deleteResponse =
                await _client.DeleteAsync(
                    $"/api/employees/{createdEmployeeId}");

            Assert.Equal(
                HttpStatusCode.NoContent,
                deleteResponse.StatusCode);

            using var getResponse =
                await _client.GetAsync(
                    $"/api/employees/{createdEmployeeId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getResponse.StatusCode);

            var employee =
                await getResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        JsonOptions);

            Assert.NotNull(
                employee);

            Assert.Equal(
                createdEmployeeId,
                employee!.Id);

            Assert.False(
                employee.IsActive);

            using var scope =
                _factory.Services.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            var persistedEmployee =
                await dbContext.Employees
                    .AsNoTracking()
                    .SingleAsync(
                        persisted =>
                            persisted.Id ==
                            createdEmployeeId);

            Assert.False(
                persistedEmployee.IsActive);

            Assert.NotNull(
                persistedEmployee.UpdatedAtUtc);
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
