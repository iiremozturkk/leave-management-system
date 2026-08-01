using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.LeaveRequests.Persistence;

internal static class LeaveBalanceQueries
{
    internal static async Task<int> GetApprovedUsedDaysForYearAsync(
        AppDbContext dbContext,
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        Guid? excludedLeaveRequestId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            dbContext);

        var yearStart =
            new DateOnly(
                year,
                1,
                1);

        var yearEnd =
            new DateOnly(
                year,
                12,
                31);

        var approvedLeaveRequestsInYear =
            await dbContext.LeaveRequests
                .AsNoTracking()
                .Where(
                    leaveRequest =>
                        leaveRequest.EmployeeId == employeeId
                        && leaveRequest.LeaveTypeId == leaveTypeId
                        && leaveRequest.Status ==
                            LeaveRequestStatus.Approved
                        && leaveRequest.StartDate <= yearEnd
                        && leaveRequest.EndDate >= yearStart
                        && (!excludedLeaveRequestId.HasValue
                            || leaveRequest.Id !=
                                excludedLeaveRequestId.Value))
                .Select(
                    leaveRequest => new
                    {
                        leaveRequest.StartDate,
                        leaveRequest.EndDate
                    })
                .ToListAsync(
                    cancellationToken);

        return approvedLeaveRequestsInYear.Sum(
            leaveRequest =>
                CalculateDaysWithinYear(
                    leaveRequest.StartDate,
                    leaveRequest.EndDate,
                    year));
    }

    private static int CalculateDaysWithinYear(
        DateOnly startDate,
        DateOnly endDate,
        int year)
    {
        var yearStart =
            new DateOnly(
                year,
                1,
                1);

        var yearEnd =
            new DateOnly(
                year,
                12,
                31);

        var effectiveStartDate =
            startDate > yearStart
                ? startDate
                : yearStart;

        var effectiveEndDate =
            endDate < yearEnd
                ? endDate
                : yearEnd;

        if (effectiveEndDate < effectiveStartDate)
        {
            return 0;
        }

        return effectiveEndDate.DayNumber
            - effectiveStartDate.DayNumber
            + 1;
    }
}
