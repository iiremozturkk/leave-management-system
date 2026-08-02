using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Domain.Entities;

namespace LeaveManagementSystem.Application.LeaveRequests.Rules;

internal static class LeaveRequestBusinessRules
{
    internal static async Task EnsureActiveEmployeeExistsAsync(
        ILeaveRequestWriteRepository writeRepository,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            writeRepository);

        if (employeeId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Employee id cannot be empty.");
        }

        var employeeExists =
            await writeRepository.ActiveEmployeeExistsAsync(
                employeeId,
                cancellationToken);

        if (!employeeExists)
        {
            throw new InvalidOperationException(
                "Employee does not exist or is not active.");
        }
    }

    internal static async Task<LeaveType> GetLeaveTypeAsync(
        ILeaveRequestWriteRepository writeRepository,
        Guid leaveTypeId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            writeRepository);

        if (leaveTypeId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Leave type id cannot be empty.");
        }

        return await writeRepository.GetLeaveTypeAsync(
                   leaveTypeId,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "Leave type does not exist.");
    }

    internal static async Task EnsureNoOverlapAsync(
        ILeaveRequestWriteRepository writeRepository,
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludedLeaveRequestId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            writeRepository);

        var hasOverlap =
            await writeRepository.HasOverlappingLeaveRequestAsync(
                employeeId,
                startDate,
                endDate,
                excludedLeaveRequestId,
                cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException(
                "Employee already has a leave request in the selected date range.");
        }
    }

    internal static async Task EnsureEnoughBalanceAsync(
        ILeaveRequestWriteRepository writeRepository,
        Guid employeeId,
        LeaveType leaveType,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludedLeaveRequestId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            writeRepository);

        ArgumentNullException.ThrowIfNull(
            leaveType);

        var requestedDaysByYear =
            LeaveRequestRules.GetRequestedDaysByYear(
                startDate,
                endDate);

        foreach (var requestedDaysForYear in requestedDaysByYear)
        {
            var usedDays =
                await writeRepository.GetApprovedUsedDaysForYearAsync(
                    employeeId,
                    leaveType.Id,
                    requestedDaysForYear.Year,
                    excludedLeaveRequestId,
                    cancellationToken);

            var entitledDays =
                LeaveRequestRules.CalculateEntitledDays(
                    leaveType.DefaultAnnualAllowanceDays,
                    requestedDaysForYear.Year);

            if (entitledDays <= 0)
            {
                continue;
            }

            var remainingDays =
                entitledDays - usedDays;

            if (requestedDaysForYear.Days > remainingDays)
            {
                throw new InvalidOperationException(
                    "Requested leave days exceed the remaining leave balance.");
            }
        }
    }
}
