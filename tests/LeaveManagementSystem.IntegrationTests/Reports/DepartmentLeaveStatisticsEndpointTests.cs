using System.Net;
using System.Net.Http.Json;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.Reports;

public sealed class DepartmentLeaveStatisticsEndpointTests(
    TestWebApplicationFactory factory)
    : HrIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetDepartmentLeaveStatistics_ReturnsCorrectApprovedAggregatesGroupedByDepartment()
    {
        await EnsureDatabaseReadyAsync();

        var departmentAId =
            await CreateNamedDepartmentAsync(
                "A Statistics Department");

        var departmentBId =
            await CreateNamedDepartmentAsync(
                "B Statistics Department");

        var departmentCId =
            await CreateNamedDepartmentAsync(
                "C Statistics Department");

        Guid? employeeAId =
            null;

        Guid? employeeBId =
            null;

        Guid? employeeCId =
            null;

        var departmentAApprovedTwoDaysId =
            Guid.NewGuid();

        var departmentAApprovedFourDaysId =
            Guid.NewGuid();

        var departmentAPendingTenDaysId =
            Guid.NewGuid();

        var departmentBApprovedFiveDaysId =
            Guid.NewGuid();

        var departmentBRejectedEightDaysId =
            Guid.NewGuid();

        var departmentCPendingThreeDaysId =
            Guid.NewGuid();

        var departmentCRejectedSixDaysId =
            Guid.NewGuid();

        try
        {
            employeeAId =
                await CreateEmployeeViaApiAsync(
                    departmentAId);

            employeeBId =
                await CreateEmployeeViaApiAsync(
                    departmentBId);

            employeeCId =
                await CreateEmployeeViaApiAsync(
                    departmentCId);

            var departmentAApprovedTwoDays =
                CreateLeaveRequest(
                    departmentAApprovedTwoDaysId,
                    employeeAId.Value,
                    "Department A approved two-day request.",
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 1, 2));

            departmentAApprovedTwoDays.Approve(
                HrEmployeeId,
                "Approved for statistics test.");

            var departmentAApprovedFourDays =
                CreateLeaveRequest(
                    departmentAApprovedFourDaysId,
                    employeeAId.Value,
                    "Department A approved four-day request.",
                    new DateOnly(2026, 2, 1),
                    new DateOnly(2026, 2, 4));

            departmentAApprovedFourDays.Approve(
                HrEmployeeId,
                "Approved for statistics test.");

            var departmentAPendingTenDays =
                CreateLeaveRequest(
                    departmentAPendingTenDaysId,
                    employeeAId.Value,
                    "Department A pending ten-day request.",
                    new DateOnly(2026, 3, 1),
                    new DateOnly(2026, 3, 10));

            var departmentBApprovedFiveDays =
                CreateLeaveRequest(
                    departmentBApprovedFiveDaysId,
                    employeeBId.Value,
                    "Department B approved five-day request.",
                    new DateOnly(2026, 4, 1),
                    new DateOnly(2026, 4, 5));

            departmentBApprovedFiveDays.Approve(
                HrEmployeeId,
                "Approved for statistics test.");

            var departmentBRejectedEightDays =
                CreateLeaveRequest(
                    departmentBRejectedEightDaysId,
                    employeeBId.Value,
                    "Department B rejected eight-day request.",
                    new DateOnly(2026, 5, 1),
                    new DateOnly(2026, 5, 8));

            departmentBRejectedEightDays.Reject(
                HrEmployeeId,
                "Rejected for statistics test.");

            var departmentCPendingThreeDays =
                CreateLeaveRequest(
                    departmentCPendingThreeDaysId,
                    employeeCId.Value,
                    "Department C pending three-day request.",
                    new DateOnly(2026, 6, 1),
                    new DateOnly(2026, 6, 3));

            var departmentCRejectedSixDays =
                CreateLeaveRequest(
                    departmentCRejectedSixDaysId,
                    employeeCId.Value,
                    "Department C rejected six-day request.",
                    new DateOnly(2026, 7, 1),
                    new DateOnly(2026, 7, 6));

            departmentCRejectedSixDays.Reject(
                HrEmployeeId,
                "Rejected for statistics test.");

            using (var scope =
                   _factory.Services.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                dbContext.LeaveRequests.AddRange(
                    departmentAApprovedTwoDays,
                    departmentAApprovedFourDays,
                    departmentAPendingTenDays,
                    departmentBApprovedFiveDays,
                    departmentBRejectedEightDays,
                    departmentCPendingThreeDays,
                    departmentCRejectedSixDays);

                await dbContext.SaveChangesAsync();
            }

            using var response =
                await HrClient.GetAsync(
                    "/api/reports/department-leave-statistics");

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var statistics =
                await response.Content
                    .ReadFromJsonAsync<
                        List<DepartmentLeaveStatisticsResponse>>(
                        JsonOptions);

            Assert.NotNull(
                statistics);

            var departmentAStatistics =
                Assert.Single(
                    statistics!,
                    item =>
                        item.DepartmentId ==
                        departmentAId);

            Assert.Equal(
                "A Statistics Department",
                departmentAStatistics.DepartmentName);

            Assert.Equal(
                2,
                departmentAStatistics.ApprovedRequestCount);

            Assert.Equal(
                6,
                departmentAStatistics.TotalApprovedLeaveDays);

            Assert.Equal(
                3m,
                departmentAStatistics
                    .AverageApprovedLeaveDaysPerRequest);

            var departmentBStatistics =
                Assert.Single(
                    statistics,
                    item =>
                        item.DepartmentId ==
                        departmentBId);

            Assert.Equal(
                "B Statistics Department",
                departmentBStatistics.DepartmentName);

            Assert.Equal(
                1,
                departmentBStatistics.ApprovedRequestCount);

            Assert.Equal(
                5,
                departmentBStatistics.TotalApprovedLeaveDays);

            Assert.Equal(
                5m,
                departmentBStatistics
                    .AverageApprovedLeaveDaysPerRequest);

            Assert.DoesNotContain(
                statistics,
                item =>
                    item.DepartmentId ==
                    departmentCId);

            var departmentAIndex =
                statistics.FindIndex(
                    item =>
                        item.DepartmentId ==
                        departmentAId);

            var departmentBIndex =
                statistics.FindIndex(
                    item =>
                        item.DepartmentId ==
                        departmentBId);

            Assert.True(
                departmentAIndex >= 0);

            Assert.True(
                departmentBIndex >= 0);

            Assert.True(
                departmentAIndex <
                departmentBIndex);
        }
        finally
        {
            using (var scope =
                   _factory.Services.CreateScope())
            {
                var dbContext =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                var leaveRequestIds =
                    new[]
                    {
                        departmentAApprovedTwoDaysId,
                        departmentAApprovedFourDaysId,
                        departmentAPendingTenDaysId,
                        departmentBApprovedFiveDaysId,
                        departmentBRejectedEightDaysId,
                        departmentCPendingThreeDaysId,
                        departmentCRejectedSixDaysId
                    };

                var leaveRequests =
                    await dbContext.LeaveRequests
                        .Where(
                            leaveRequest =>
                                leaveRequestIds.Contains(
                                    leaveRequest.Id))
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
                employeeId: employeeCId,
                departmentId: departmentCId);

            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeBId,
                departmentId: departmentBId);

            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeAId,
                departmentId: departmentAId);
        }
    }

    private async Task<Guid> CreateNamedDepartmentAsync(
        string name)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var department =
            new Department
            {
                Id =
                    Guid.NewGuid(),

                Name =
                    name,

                CreatedAtUtc =
                    DateTime.UtcNow,

                UpdatedAtUtc =
                    null
            };

        dbContext.Departments.Add(
            department);

        await dbContext.SaveChangesAsync();

        return department.Id;
    }

    private static LeaveRequest CreateLeaveRequest(
        Guid leaveRequestId,
        Guid employeeId,
        string reason,
        DateOnly startDate,
        DateOnly endDate)
    {
        var leaveRequest =
            new LeaveRequest
            {
                Id =
                    leaveRequestId,

                EmployeeId =
                    employeeId,

                LeaveTypeId =
                    AnnualLeaveTypeId,

                Reason =
                    reason,

                CreatedAtUtc =
                    DateTime.UtcNow,

                UpdatedAtUtc =
                    null
            };

        leaveRequest.SetDateRange(
            startDate,
            endDate);

        return leaveRequest;
    }
}
