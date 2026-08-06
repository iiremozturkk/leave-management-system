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
        return await dbContext.LeaveRequests
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
                    new DepartmentLeaveStatisticsDto(
                        group.Key.DepartmentId,
                        group.Key.DepartmentName,
                        group.Count(),
                        group.Sum(
                            leaveRequest =>
                                leaveRequest.RequestedDays),
                        group.Average(
                            leaveRequest =>
                                (decimal)leaveRequest.RequestedDays)))
            .OrderBy(
                statistics =>
                    statistics.DepartmentName)
            .ThenBy(
                statistics =>
                    statistics.DepartmentId)
            .ToListAsync(
                cancellationToken);
    }
}
