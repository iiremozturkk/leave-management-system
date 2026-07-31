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
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetLeaveRequests_ReturnsProjectedRequestsOrderedByCreatedAtDescending()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        var olderLeaveRequestId =
            Guid.NewGuid();

        var newerLeaveRequestId =
            Guid.NewGuid();

        try
        {
            employeeId =
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

            var olderLeaveRequest =
                new LeaveRequest
                {
                    Id = olderLeaveRequestId,
                    EmployeeId = employeeId.Value,
                    LeaveTypeId = AnnualLeaveTypeId,
                    Reason = "Older integration leave request.",
                    CreatedAtUtc = olderCreatedAtUtc,
                    UpdatedAtUtc = null
                };

            olderLeaveRequest.SetDateRange(
                new DateOnly(2026, 9, 10),
                new DateOnly(2026, 9, 11));

            var newerLeaveRequest =
                new LeaveRequest
                {
                    Id = newerLeaveRequestId,
                    EmployeeId = employeeId.Value,
                    LeaveTypeId = AnnualLeaveTypeId,
                    Reason = "Newer integration leave request.",
                    CreatedAtUtc = newerCreatedAtUtc,
                    UpdatedAtUtc = null
                };

            newerLeaveRequest.SetDateRange(
                new DateOnly(2026, 10, 20),
                new DateOnly(2026, 10, 22));

            using (var scope =
                   _factory.Services.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                dbContext.LeaveRequests.AddRange(
                    olderLeaveRequest,
                    newerLeaveRequest);

                await dbContext.SaveChangesAsync();
            }

            using var response =
                await _client.GetAsync(
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

            var olderIndex =
                leaveRequests!.FindIndex(
                    leaveRequest =>
                        leaveRequest.Id ==
                        olderLeaveRequestId);

            var newerIndex =
                leaveRequests.FindIndex(
                    leaveRequest =>
                        leaveRequest.Id ==
                        newerLeaveRequestId);

            Assert.True(
                olderIndex >= 0);

            Assert.True(
                newerIndex >= 0);

            Assert.True(
                newerIndex < olderIndex);

            var projectedLeaveRequest =
                leaveRequests[newerIndex];

            Assert.Equal(
                newerLeaveRequestId,
                projectedLeaveRequest.Id);

            Assert.Equal(
                employeeId.Value,
                projectedLeaveRequest.EmployeeId);

            Assert.Equal(
                "Integration LeaveEmployee",
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
                "Newer integration leave request.",
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
                                olderLeaveRequestId
                                || leaveRequest.Id ==
                                newerLeaveRequestId)
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
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }
}
