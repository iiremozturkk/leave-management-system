using System.Net;
using System.Net.Http.Json;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.LeaveRequests;

public sealed class GetLeaveRequestByIdEndpointTests(
    TestWebApplicationFactory factory)
    : HrIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetLeaveRequestById_OwnLeaveRequestExists_ReturnsProjectedLeaveRequest()
    {
        await EnsureDatabaseReadyAsync();

        Guid? leaveRequestId = null;

        try
        {
            var createRequest = new
            {
                leaveTypeId =
                    AnnualLeaveTypeId,

                startDate =
                    "2026-11-10",

                endDate =
                    "2026-11-12",

                reason =
                    "Get by id integration test."
            };

            using var createResponse =
                await HrClient.PostAsJsonAsync(
                    "/api/leave-requests",
                    createRequest);

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var createdLeaveRequest =
                await createResponse.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(
                createdLeaveRequest);

            leaveRequestId =
                createdLeaveRequest!.Id;

            Assert.Equal(
                HrEmployeeId,
                createdLeaveRequest.EmployeeId);

            using var response =
                await HrClient.GetAsync(
                    $"/api/leave-requests/{leaveRequestId.Value}");

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var leaveRequest =
                await response.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(
                leaveRequest);

            Assert.Equal(
                leaveRequestId.Value,
                leaveRequest!.Id);

            Assert.Equal(
                HrEmployeeId,
                leaveRequest.EmployeeId);

            Assert.Equal(
                "Integration HrAdministrator",
                leaveRequest.EmployeeFullName);

            Assert.Equal(
                AnnualLeaveTypeId,
                leaveRequest.LeaveTypeId);

            Assert.Equal(
                "Annual Leave",
                leaveRequest.LeaveTypeName);

            Assert.Equal(
                new DateOnly(2026, 11, 10),
                leaveRequest.StartDate);

            Assert.Equal(
                new DateOnly(2026, 11, 12),
                leaveRequest.EndDate);

            Assert.Equal(
                3,
                leaveRequest.RequestedDays);

            Assert.Equal(
                LeaveRequestStatus.Pending,
                leaveRequest.Status);

            Assert.Equal(
                "Get by id integration test.",
                leaveRequest.Reason);

            Assert.Null(
                leaveRequest.ManagerComment);

            Assert.Null(
                leaveRequest.ReviewedAtUtc);

            Assert.Null(
                leaveRequest.ReviewedByEmployeeId);

            Assert.Null(
                leaveRequest.ReviewedByEmployeeFullName);

            Assert.NotEqual(
                default,
                leaveRequest.CreatedAtUtc);

            Assert.Null(
                leaveRequest.UpdatedAtUtc);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId:
                    leaveRequestId,

                employeeId:
                    null,

                departmentId:
                    null);
        }
    }

    [Fact]
    public async Task GetLeaveRequestById_HrCanReadInactiveEmployeesHistoricalRequest()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? otherEmployeeId = null;

        var leaveRequestId =
            Guid.NewGuid();

        try
        {
            otherEmployeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            var leaveRequest =
                new LeaveRequest
                {
                    Id =
                        leaveRequestId,

                    EmployeeId =
                        otherEmployeeId.Value,

                    LeaveTypeId =
                        AnnualLeaveTypeId,

                    Reason =
                        "Another employee's leave request.",

                    CreatedAtUtc =
                        DateTime.UtcNow,

                    UpdatedAtUtc =
                        null
                };

            leaveRequest.SetDateRange(
                new DateOnly(2026, 12, 1),
                new DateOnly(2026, 12, 2));

            using (var scope =
                   _factory.Services.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                var otherEmployee =
                    await dbContext.Employees
                        .SingleAsync(
                            employee =>
                                employee.Id ==
                                otherEmployeeId.Value);

                otherEmployee.IsActive =
                    false;

                otherEmployee.UpdatedAtUtc =
                    DateTime.UtcNow;

                dbContext.LeaveRequests.Add(
                    leaveRequest);

                await dbContext.SaveChangesAsync();
            }

            using var response =
                await HrClient.GetAsync(
                    $"/api/leave-requests/{leaveRequestId}");

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var historicalLeaveRequest =
                await response.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(
                historicalLeaveRequest);

            Assert.Equal(
                leaveRequestId,
                historicalLeaveRequest!.Id);

            Assert.Equal(
                otherEmployeeId.Value,
                historicalLeaveRequest.EmployeeId);

            Assert.Equal(
                "Another employee's leave request.",
                historicalLeaveRequest.Reason);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId:
                    leaveRequestId,

                employeeId:
                    otherEmployeeId,

                departmentId:
                    departmentId);
        }
    }

    [Fact]
    public async Task GetLeaveRequestById_IdIsEmpty_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await HrClient.GetAsync(
                $"/api/leave-requests/{Guid.Empty}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}
