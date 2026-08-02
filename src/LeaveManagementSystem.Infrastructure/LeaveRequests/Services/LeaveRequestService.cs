using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Services;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.Infrastructure.LeaveRequests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.LeaveRequests.Services;

public sealed class LeaveRequestService(AppDbContext dbContext) : ILeaveRequestService
{
    private const int MinSupportedYear = 2000;
    private const int MaxSupportedYear = 2100;

    public async Task<LeaveRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(leaveRequest => leaveRequest.Id == id)
            .Select(LeaveRequestProjections.ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LeaveRequestDto?> ApproveAsync(
        Guid id,
        ReviewLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var leaveRequest = await dbContext.LeaveRequests
            .FirstOrDefaultAsync(leaveRequest => leaveRequest.Id == id, cancellationToken);

        if (leaveRequest is null)
        {
            return null;
        }

        await EnsureReviewerCanReviewAsync(
            leaveRequest,
            request.ReviewerEmployeeId,
            cancellationToken);

        EnsureSupportedDateRange(leaveRequest.StartDate, leaveRequest.EndDate);

        await EnsureEnoughLeaveBalanceAsync(
            leaveRequest.EmployeeId,
            leaveRequest.LeaveTypeId,
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.Id,
            cancellationToken);

        leaveRequest.Approve(request.ReviewerEmployeeId, request.ManagerComment);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(leaveRequest.Id, cancellationToken);
    }

    public async Task<LeaveRequestDto?> RejectAsync(
        Guid id,
        ReviewLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var leaveRequest = await dbContext.LeaveRequests
            .FirstOrDefaultAsync(leaveRequest => leaveRequest.Id == id, cancellationToken);

        if (leaveRequest is null)
        {
            return null;
        }

        await EnsureReviewerCanReviewAsync(
            leaveRequest,
            request.ReviewerEmployeeId,
            cancellationToken);

        leaveRequest.Reject(request.ReviewerEmployeeId, request.ManagerComment);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(leaveRequest.Id, cancellationToken);
    }

    private async Task EnsureEnoughLeaveBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludedLeaveRequestId,
        CancellationToken cancellationToken)
    {
        var requestedDaysByYear = GetRequestedDaysByYear(startDate, endDate);

        foreach (var requestedDaysForYear in requestedDaysByYear)
        {
            var balance = await CalculateBalanceAsync(
                employeeId,
                leaveTypeId,
                requestedDaysForYear.Year,
                excludedLeaveRequestId,
                cancellationToken);

            if (balance is null)
            {
                throw new InvalidOperationException("Leave type does not exist.");
            }

            if (balance.EntitledDays <= 0)
            {
                continue;
            }

            if (requestedDaysForYear.Days > balance.RemainingDays)
            {
                throw new InvalidOperationException(
                    "Requested leave days exceed the remaining leave balance.");
            }
        }
    }

    private async Task<LeaveBalanceDto?> CalculateBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        Guid? excludedLeaveRequestId,
        CancellationToken cancellationToken)
    {
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

    private async Task EnsureReviewerCanReviewAsync(
        LeaveRequest leaveRequest,
        Guid reviewerEmployeeId,
        CancellationToken cancellationToken)
    {
        if (reviewerEmployeeId == Guid.Empty)
        {
            throw new InvalidOperationException("Reviewer employee id cannot be empty.");
        }

        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                employee => employee.Id == leaveRequest.EmployeeId,
                cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new InvalidOperationException("Employee does not exist or is not active.");
        }

        var reviewer = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                employee => employee.Id == reviewerEmployeeId,
                cancellationToken);

        if (reviewer is null || !reviewer.IsActive)
        {
            throw new InvalidOperationException("Reviewer does not exist or is not active.");
        }

        if (reviewer.Role != EmployeeRole.Manager)
        {
            throw new ForbiddenOperationException("Only managers can review leave requests.");
        }

        if (employee.ManagerId != reviewerEmployeeId)
        {
            throw new ForbiddenOperationException(
                "Only the employee's direct manager can review this leave request.");
        }
    }

    private static void EnsureSupportedDateRange(DateOnly startDate, DateOnly endDate)
    {
        CalculateRequestedDays(startDate, endDate);
        EnsureSupportedYear(startDate.Year);
        EnsureSupportedYear(endDate.Year);
    }

    private static IReadOnlyList<(int Year, int Days)> GetRequestedDaysByYear(
        DateOnly startDate,
        DateOnly endDate)
    {
        EnsureSupportedDateRange(startDate, endDate);

        var requestedDaysByYear = new List<(int Year, int Days)>();

        for (var year = startDate.Year; year <= endDate.Year; year++)
        {
            var daysInYear = CalculateDaysWithinYear(startDate, endDate, year);

            if (daysInYear > 0)
            {
                requestedDaysByYear.Add((year, daysInYear));
            }
        }

        return requestedDaysByYear;
    }

    private static int CalculateDaysWithinYear(
        DateOnly startDate,
        DateOnly endDate,
        int year)
    {
        EnsureSupportedYear(year);

        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);

        var effectiveStartDate = startDate > yearStart
            ? startDate
            : yearStart;

        var effectiveEndDate = endDate < yearEnd
            ? endDate
            : yearEnd;

        if (effectiveEndDate < effectiveStartDate)
        {
            return 0;
        }

        return CalculateRequestedDays(effectiveStartDate, effectiveEndDate);
    }

    private static int CalculateEntitledDays(LeaveType leaveType, int year)
    {
        _ = year; // year is currently unused; reserved in case future per-year entitlement rules are added.

        return leaveType.DefaultAnnualAllowanceDays;
    }

    private static void EnsureSupportedYear(int year)
    {
        if (year < MinSupportedYear || year > MaxSupportedYear)
        {
            throw new InvalidOperationException(
                $"Year must be between {MinSupportedYear} and {MaxSupportedYear}.");
        }
    }

    private static int CalculateRequestedDays(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new InvalidOperationException("End date cannot be earlier than start date.");
        }

        return endDate.DayNumber - startDate.DayNumber + 1;
    }
}
