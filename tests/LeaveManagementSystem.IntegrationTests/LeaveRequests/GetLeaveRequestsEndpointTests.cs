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

public sealed class GetLeaveRequestsEndpointTests(
    TestWebApplicationFactory factory)
    : HrIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetLeaveRequests_HrReturnsAllRequestsIncludingInactiveEmployeeHistoryOrderedByCreatedAtDescending()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? otherEmployeeId = null;

        var olderOwnLeaveRequestId =
            Guid.NewGuid();

        var newerOwnLeaveRequestId =
            Guid.NewGuid();

        var otherEmployeesLeaveRequestId =
            Guid.NewGuid();

        try
        {
            otherEmployeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            var olderCreatedAtUtc =
                new DateTime(
                    2026,
                    8,
                    1,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc);

            var newerCreatedAtUtc =
                new DateTime(
                    2026,
                    8,
                    1,
                    11,
                    0,
                    0,
                    DateTimeKind.Utc);

            var otherEmployeesCreatedAtUtc =
                new DateTime(
                    2026,
                    8,
                    1,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc);

            var olderOwnLeaveRequest =
                new LeaveRequest
                {
                    Id =
                        olderOwnLeaveRequestId,
                    EmployeeId =
                        HrEmployeeId,
                    LeaveTypeId =
                        AnnualLeaveTypeId,
                    Reason =
                        "Older own integration leave request.",
                    CreatedAtUtc =
                        olderCreatedAtUtc,
                    UpdatedAtUtc =
                        null
                };

            olderOwnLeaveRequest.SetDateRange(
                new DateOnly(2026, 9, 10),
                new DateOnly(2026, 9, 11));

            var newerOwnLeaveRequest =
                new LeaveRequest
                {
                    Id =
                        newerOwnLeaveRequestId,
                    EmployeeId =
                        HrEmployeeId,
                    LeaveTypeId =
                        AnnualLeaveTypeId,
                    Reason =
                        "Newer own integration leave request.",
                    CreatedAtUtc =
                        newerCreatedAtUtc,
                    UpdatedAtUtc =
                        null
                };

            newerOwnLeaveRequest.SetDateRange(
                new DateOnly(2026, 10, 20),
                new DateOnly(2026, 10, 22));

            var otherEmployeesLeaveRequest =
                new LeaveRequest
                {
                    Id =
                        otherEmployeesLeaveRequestId,
                    EmployeeId =
                        otherEmployeeId.Value,
                    LeaveTypeId =
                        AnnualLeaveTypeId,
                    Reason =
                        "Another employee's leave request.",
                    CreatedAtUtc =
                        otherEmployeesCreatedAtUtc,
                    UpdatedAtUtc =
                        null
                };

            otherEmployeesLeaveRequest.SetDateRange(
                new DateOnly(2026, 11, 10),
                new DateOnly(2026, 11, 12));

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

                dbContext.LeaveRequests.AddRange(
                    olderOwnLeaveRequest,
                    newerOwnLeaveRequest,
                    otherEmployeesLeaveRequest);

                await dbContext.SaveChangesAsync();
            }

            using var response =
                await HrClient.GetAsync(
                    "/api/leave-requests");

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var leaveRequests =
                await response.Content
                    .ReadFromJsonAsync<
                        List<LeaveRequestResponse>>(
                        JsonOptions);

            Assert.NotNull(
                leaveRequests);

            Assert.Contains(
                leaveRequests!,
                leaveRequest =>
                    leaveRequest.Id ==
                    olderOwnLeaveRequestId);

            Assert.Contains(
                leaveRequests,
                leaveRequest =>
                    leaveRequest.Id ==
                    newerOwnLeaveRequestId);

            Assert.Contains(
                leaveRequests,
                leaveRequest =>
                    leaveRequest.Id ==
                    otherEmployeesLeaveRequestId);

            var olderIndex =
                leaveRequests.FindIndex(
                    leaveRequest =>
                        leaveRequest.Id ==
                        olderOwnLeaveRequestId);

            var newerIndex =
                leaveRequests.FindIndex(
                    leaveRequest =>
                        leaveRequest.Id ==
                        newerOwnLeaveRequestId);

            var historicalIndex =
                leaveRequests.FindIndex(
                    leaveRequest =>
                        leaveRequest.Id ==
                        otherEmployeesLeaveRequestId);

            Assert.True(
                olderIndex >= 0);

            Assert.True(
                newerIndex >= 0);

            Assert.True(
                historicalIndex >= 0);

            Assert.True(
                historicalIndex < newerIndex);

            Assert.True(
                newerIndex < olderIndex);

            var projectedLeaveRequest =
                leaveRequests[newerIndex];

            var historicalLeaveRequest =
                leaveRequests[historicalIndex];

            Assert.Equal(
                otherEmployeesLeaveRequestId,
                historicalLeaveRequest.Id);

            Assert.Equal(
                otherEmployeeId.Value,
                historicalLeaveRequest.EmployeeId);

            Assert.Equal(
                newerOwnLeaveRequestId,
                projectedLeaveRequest.Id);

            Assert.Equal(
                HrEmployeeId,
                projectedLeaveRequest.EmployeeId);

            Assert.Equal(
                "Integration HrAdministrator",
                projectedLeaveRequest.EmployeeFullName);

            Assert.Equal(
                AnnualLeaveTypeId,
                projectedLeaveRequest.LeaveTypeId);

            Assert.Equal(
                "Annual Leave",
                projectedLeaveRequest.LeaveTypeName);

            Assert.Equal(
                new DateOnly(2026, 10, 20),
                projectedLeaveRequest.StartDate);

            Assert.Equal(
                new DateOnly(2026, 10, 22),
                projectedLeaveRequest.EndDate);

            Assert.Equal(
                3,
                projectedLeaveRequest.RequestedDays);

            Assert.Equal(
                LeaveRequestStatus.Pending,
                projectedLeaveRequest.Status);

            Assert.Equal(
                "Newer own integration leave request.",
                projectedLeaveRequest.Reason);

            Assert.Null(
                projectedLeaveRequest.ManagerComment);

            Assert.Null(
                projectedLeaveRequest.ReviewedAtUtc);

            Assert.Null(
                projectedLeaveRequest.ReviewedByEmployeeId);

            Assert.Null(
                projectedLeaveRequest
                    .ReviewedByEmployeeFullName);

            Assert.Equal(
                newerCreatedAtUtc,
                projectedLeaveRequest.CreatedAtUtc);

            Assert.Null(
                projectedLeaveRequest.UpdatedAtUtc);
        }
        finally
        {
            using (var scope =
                   _factory.Services.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                var leaveRequests =
                    await dbContext.LeaveRequests
                        .Where(
                            leaveRequest =>
                                leaveRequest.Id ==
                                olderOwnLeaveRequestId
                                || leaveRequest.Id ==
                                newerOwnLeaveRequestId
                                || leaveRequest.Id ==
                                otherEmployeesLeaveRequestId)
                        .ToListAsync();

                if (leaveRequests.Count > 0)
                {
                    dbContext.LeaveRequests.RemoveRange(
                        leaveRequests);

                    await dbContext.SaveChangesAsync();
                }
            }

            await CleanupAsync(
                leaveRequestId: null,
                employeeId: otherEmployeeId,
                departmentId: departmentId);
        }
    }
}
