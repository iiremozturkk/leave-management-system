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

public sealed class CrudEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly Guid AnnualLeaveTypeId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CrudEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
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
            var listBeforeCreateResponse = await _client.GetAsync("/api/employees");

            Assert.Equal(HttpStatusCode.OK, listBeforeCreateResponse.StatusCode);

            var suffix = Guid.NewGuid().ToString("N");
            var email = $"integration.employee.{suffix}@example.com";

            var createRequest = new
            {
                firstName = "Integration",
                lastName = "Employee",
                email,
                departmentId,
                managerId = (Guid?)null,
                role = EmployeeRole.Employee
            };

            var createResponse = await _client.PostAsJsonAsync("/api/employees", createRequest);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdEmployee = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>(JsonOptions);

            Assert.NotNull(createdEmployee);

            employeeId = createdEmployee!.Id;

            Assert.Equal("Integration", createdEmployee.FirstName);
            Assert.Equal("Employee", createdEmployee.LastName);
            Assert.Equal(email, createdEmployee.Email);
            Assert.Equal(departmentId, createdEmployee.DepartmentId);
            Assert.Equal(EmployeeRole.Employee, createdEmployee.Role);
            Assert.True(createdEmployee.IsActive);

            var getByIdResponse = await _client.GetAsync($"/api/employees/{employeeId}");

            Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

            var loadedEmployee = await getByIdResponse.Content.ReadFromJsonAsync<EmployeeResponse>(JsonOptions);

            Assert.NotNull(loadedEmployee);
            Assert.Equal(employeeId, loadedEmployee!.Id);

            var listAfterCreateResponse = await _client.GetAsync("/api/employees");

            Assert.Equal(HttpStatusCode.OK, listAfterCreateResponse.StatusCode);

            var employees = await listAfterCreateResponse.Content.ReadFromJsonAsync<List<EmployeeResponse>>(JsonOptions);

            Assert.NotNull(employees);
            Assert.Contains(employees!, employee => employee.Id == employeeId);

            var updatedEmail = $"integration.employee.updated.{suffix}@example.com";

            var updateRequest = new
            {
                firstName = "Integration",
                lastName = "EmployeeUpdated",
                email = updatedEmail,
                departmentId,
                managerId = (Guid?)null,
                role = EmployeeRole.Employee,
                isActive = true
            };

            var updateResponse = await _client.PutAsJsonAsync($"/api/employees/{employeeId}", updateRequest);

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var updatedEmployee = await updateResponse.Content.ReadFromJsonAsync<EmployeeResponse>(JsonOptions);

            Assert.NotNull(updatedEmployee);
            Assert.Equal("EmployeeUpdated", updatedEmployee!.LastName);
            Assert.Equal(updatedEmail, updatedEmployee.Email);
            Assert.Equal(EmployeeRole.Employee, updatedEmployee.Role);

            var deleteResponse = await _client.DeleteAsync($"/api/employees/{employeeId}");

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }
        finally
        {
            await CleanupAsync(leaveRequestId: null, employeeId, departmentId);
        }
    }

    [Fact]
    public async Task LeaveRequestCrudFlow_WorksThroughApi()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId = await CreateDepartmentAsync();
        Guid? employeeId = null;
        Guid? leaveRequestId = null;

        try
        {
            employeeId = await CreateEmployeeViaApiAsync(departmentId);

            var listBeforeCreateResponse = await _client.GetAsync("/api/leave-requests");

            Assert.Equal(HttpStatusCode.OK, listBeforeCreateResponse.StatusCode);

            var createRequest = new
            {
                employeeId,
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-07-15",
                endDate = "2026-07-17",
                reason = "Integration test leave request."
            };

            var createResponse = await _client.PostAsJsonAsync("/api/leave-requests", createRequest);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdLeaveRequest = await createResponse.Content.ReadFromJsonAsync<LeaveRequestResponse>(JsonOptions);

            Assert.NotNull(createdLeaveRequest);

            leaveRequestId = createdLeaveRequest!.Id;

            Assert.Equal(employeeId, createdLeaveRequest.EmployeeId);
            Assert.Equal(AnnualLeaveTypeId, createdLeaveRequest.LeaveTypeId);
            Assert.Equal(3, createdLeaveRequest.RequestedDays);
            Assert.Equal(LeaveRequestStatus.Pending, createdLeaveRequest.Status);
            Assert.Equal("Integration test leave request.", createdLeaveRequest.Reason);

            var getByIdResponse = await _client.GetAsync($"/api/leave-requests/{leaveRequestId}");

            Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

            var loadedLeaveRequest = await getByIdResponse.Content.ReadFromJsonAsync<LeaveRequestResponse>(JsonOptions);

            Assert.NotNull(loadedLeaveRequest);
            Assert.Equal(leaveRequestId, loadedLeaveRequest!.Id);

            var listAfterCreateResponse = await _client.GetAsync("/api/leave-requests");

            Assert.Equal(HttpStatusCode.OK, listAfterCreateResponse.StatusCode);

            var leaveRequests = await listAfterCreateResponse.Content.ReadFromJsonAsync<List<LeaveRequestResponse>>(JsonOptions);

            Assert.NotNull(leaveRequests);
            Assert.Contains(leaveRequests!, leaveRequest => leaveRequest.Id == leaveRequestId);

            var updateRequest = new
            {
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-07-16",
                endDate = "2026-07-18",
                reason = "Updated integration test leave request."
            };

            var updateResponse = await _client.PutAsJsonAsync($"/api/leave-requests/{leaveRequestId}", updateRequest);

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var updatedLeaveRequest = await updateResponse.Content.ReadFromJsonAsync<LeaveRequestResponse>(JsonOptions);

            Assert.NotNull(updatedLeaveRequest);
            Assert.Equal(3, updatedLeaveRequest!.RequestedDays);
            Assert.Equal(LeaveRequestStatus.Pending, updatedLeaveRequest.Status);
            Assert.Equal("Updated integration test leave request.", updatedLeaveRequest.Reason);

            var deleteResponse = await _client.DeleteAsync($"/api/leave-requests/{leaveRequestId}");

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            leaveRequestId = null;
        }
        finally
        {
            await CleanupAsync(leaveRequestId, employeeId, departmentId);
        }
    }

    private async Task EnsureDatabaseReadyAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();

        var annualLeaveExists = await dbContext.LeaveTypes
            .AnyAsync(leaveType => leaveType.Id == AnnualLeaveTypeId);

        Assert.True(annualLeaveExists, "The default Annual Leave type should exist in the test database.");
    }

    private async Task<Guid> CreateDepartmentAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = $"Integration Test Department {Guid.NewGuid():N}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync();

        return department.Id;
    }

    private async Task<Guid> CreateEmployeeViaApiAsync(Guid departmentId)
    {
        var suffix = Guid.NewGuid().ToString("N");

        var createRequest = new
        {
            firstName = "Integration",
            lastName = "LeaveEmployee",
            email = $"integration.leave.employee.{suffix}@example.com",
            departmentId,
            managerId = (Guid?)null,
            role = EmployeeRole.Employee
        };

        var createResponse = await _client.PostAsJsonAsync("/api/employees", createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdEmployee = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>(JsonOptions);

        Assert.NotNull(createdEmployee);

        return createdEmployee!.Id;
    }

    private async Task CleanupAsync(Guid? leaveRequestId, Guid? employeeId, Guid? departmentId)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (leaveRequestId is not null)
        {
            var leaveRequest = await dbContext.LeaveRequests
                .FirstOrDefaultAsync(request => request.Id == leaveRequestId.Value);

            if (leaveRequest is not null)
            {
                dbContext.LeaveRequests.Remove(leaveRequest);
            }
        }

        if (employeeId is not null)
        {
            var employee = await dbContext.Employees
                .FirstOrDefaultAsync(employee => employee.Id == employeeId.Value);

            if (employee is not null)
            {
                dbContext.Employees.Remove(employee);
            }
        }

        if (departmentId is not null)
        {
            var department = await dbContext.Departments
                .FirstOrDefaultAsync(department => department.Id == departmentId.Value);

            if (department is not null)
            {
                dbContext.Departments.Remove(department);
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
}
