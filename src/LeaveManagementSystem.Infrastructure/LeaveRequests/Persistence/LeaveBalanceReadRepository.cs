using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.LeaveRequests.Persistence;

public sealed class LeaveBalanceReadRepository(
    AppDbContext dbContext)
    : ILeaveBalanceReadRepository
{
    public async Task<LeaveBalanceDto?> GetBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        Guid? excludedLeaveRequestId = null,
        CancellationToken cancellationToken = default)
    {
        var employeeExists =
            await dbContext.Employees
                .AsNoTracking()
                .AnyAsync(
                    employee =>
                        employee.Id == employeeId
                        && employee.IsActive,
                    cancellationToken);

        if (!employeeExists)
        {
            return null;
        }

        var leaveType =
            await dbContext.LeaveTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    leaveType =>
                        leaveType.Id == leaveTypeId,
                    cancellationToken);

        if (leaveType is null)
        {
            return null;
        }

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

        var usedDays =
            approvedLeaveRequestsInYear.Sum(
                leaveRequest =>
                    CalculateDaysWithinYear(
                        leaveRequest.StartDate,
                        leaveRequest.EndDate,
                        year));

        var entitledDays =
            CalculateEntitledDays(
                leaveType,
                year);

        var remainingDays =
            entitledDays - usedDays;

        return new LeaveBalanceDto(
            employeeId,
            leaveType.Id,
            leaveType.Name,
            year,
            entitledDays,
            usedDays,
            remainingDays);
    }

    private static int CalculateEntitledDays(
        LeaveType leaveType,
        int year)
    {
        _ = year; // Reserved for future year-specific entitlement rules.

        return leaveType.DefaultAnnualAllowanceDays;
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
