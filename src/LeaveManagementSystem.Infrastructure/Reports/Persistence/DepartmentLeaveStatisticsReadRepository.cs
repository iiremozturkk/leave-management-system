using LeaveManagementSystem.Application.Reports.Abstractions;
using LeaveManagementSystem.Application.Reports.Dtos;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.Reports.Persistence;

public sealed class DepartmentLeaveStatisticsReadRepository(
    AppDbContext dbContext)
    : IDepartmentLeaveStatisticsReadRepository
{
    public async Task<IReadOnlyList<DepartmentLeaveStatisticsDto>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var statistics =
            await dbContext.LeaveRequests
                .AsNoTracking()
                .Where(
                    leaveRequest =>
                        leaveRequest.Status ==
                        LeaveRequestStatus.Approved)
                .GroupBy(
                    leaveRequest =>
                        new
                        {
                            DepartmentId =
                                leaveRequest.Employee.DepartmentId,

                            DepartmentName =
                                leaveRequest.Employee.Department.Name
                        })
                .Select(
                    group =>
                        new
                        {
                            group.Key.DepartmentId,
                            group.Key.DepartmentName,

                            ApprovedRequestCount =
                                group.Count(),

                            TotalApprovedLeaveDays =
                                group.Sum(
                                    leaveRequest =>
                                        leaveRequest.RequestedDays),

                            AverageApprovedLeaveDaysPerRequest =
                                group.Average(
                                    leaveRequest =>
                                        leaveRequest.RequestedDays)
                        })
                .OrderBy(
                    item =>
                        item.DepartmentName)
                .ThenBy(
                    item =>
                        item.DepartmentId)
                .ToListAsync(
                    cancellationToken);

        return statistics
            .Select(
                item =>
                    new DepartmentLeaveStatisticsDto(
                        item.DepartmentId,
                        item.DepartmentName,
                        item.ApprovedRequestCount,
                        item.TotalApprovedLeaveDays,
                        (decimal)item
                            .AverageApprovedLeaveDaysPerRequest))
            .ToList();
    }
}
