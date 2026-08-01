using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Domain.Entities;
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

        var usedDays =
            await LeaveBalanceQueries
                .GetApprovedUsedDaysForYearAsync(
                    dbContext,
                    employeeId,
                    leaveTypeId,
                    year,
                    excludedLeaveRequestId,
                    cancellationToken);

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
}
