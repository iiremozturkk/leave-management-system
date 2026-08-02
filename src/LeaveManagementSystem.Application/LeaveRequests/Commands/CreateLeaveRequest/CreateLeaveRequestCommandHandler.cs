using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Domain.Entities;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.CreateLeaveRequest;

public sealed class CreateLeaveRequestCommandHandler(
    ILeaveRequestWriteRepository writeRepository,
    ILeaveRequestReadRepository readRepository)
    : IRequestHandler<CreateLeaveRequestCommand, LeaveRequestDto>
{
    private const int ReasonMaxLength = 500;
    private const int MinSupportedYear = 2000;
    private const int MaxSupportedYear = 2100;

    public async Task<LeaveRequestDto> Handle(
        CreateLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reason =
            NormalizeRequiredText(
                request.Reason,
                "Reason",
                ReasonMaxLength);

        EnsureSupportedDateRange(
            request.StartDate,
            request.EndDate);

        await EnsureEmployeeExistsAndIsActiveAsync(
            request.EmployeeId,
            cancellationToken);

        var leaveType =
            await GetLeaveTypeAsync(
                request.LeaveTypeId,
                cancellationToken);

        await EnsureNoOverlappingLeaveRequestAsync(
            request.EmployeeId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        await EnsureEnoughLeaveBalanceAsync(
            request.EmployeeId,
            leaveType,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        var leaveRequest =
            new LeaveRequest
            {
                EmployeeId = request.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,
                Reason = reason
            };

        leaveRequest.SetDateRange(
            request.StartDate,
            request.EndDate);

        writeRepository.Add(
            leaveRequest);

        await writeRepository.SaveChangesAsync(
            cancellationToken);

        return await readRepository.GetByIdAsync(
                   leaveRequest.Id,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "Leave request was created but could not be loaded.");
    }

    private async Task EnsureEmployeeExistsAndIsActiveAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
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

    private async Task<LeaveType> GetLeaveTypeAsync(
        Guid leaveTypeId,
        CancellationToken cancellationToken)
    {
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

    private async Task EnsureNoOverlappingLeaveRequestAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var hasOverlap =
            await writeRepository.HasOverlappingLeaveRequestAsync(
                employeeId,
                startDate,
                endDate,
                excludedLeaveRequestId: null,
                cancellationToken: cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException(
                "Employee already has a leave request in the selected date range.");
        }
    }

    private async Task EnsureEnoughLeaveBalanceAsync(
        Guid employeeId,
        LeaveType leaveType,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var requestedDaysByYear =
            GetRequestedDaysByYear(
                startDate,
                endDate);

        foreach (var requestedDaysForYear in requestedDaysByYear)
        {
            var usedDays =
                await writeRepository.GetApprovedUsedDaysForYearAsync(
                    employeeId,
                    leaveType.Id,
                    requestedDaysForYear.Year,
                    excludedLeaveRequestId: null,
                    cancellationToken: cancellationToken);

            var entitledDays =
                CalculateEntitledDays(
                    leaveType,
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

    private static IReadOnlyList<(int Year, int Days)>
        GetRequestedDaysByYear(
            DateOnly startDate,
            DateOnly endDate)
    {
        var requestedDaysByYear =
            new List<(int Year, int Days)>();

        for (var year = startDate.Year;
             year <= endDate.Year;
             year++)
        {
            var daysInYear =
                CalculateDaysWithinYear(
                    startDate,
                    endDate,
                    year);

            if (daysInYear > 0)
            {
                requestedDaysByYear.Add(
                    (year, daysInYear));
            }
        }

        return requestedDaysByYear;
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

        return CalculateRequestedDays(
            effectiveStartDate,
            effectiveEndDate);
    }

    private static int CalculateEntitledDays(
        LeaveType leaveType,
        int year)
    {
        _ = year; // Reserved for future year-specific entitlement rules.

        return leaveType.DefaultAnnualAllowanceDays;
    }

    private static void EnsureSupportedDateRange(
        DateOnly startDate,
        DateOnly endDate)
    {
        CalculateRequestedDays(
            startDate,
            endDate);

        EnsureSupportedYear(
            startDate.Year);

        EnsureSupportedYear(
            endDate.Year);
    }

    private static void EnsureSupportedYear(
        int year)
    {
        if (year < MinSupportedYear
            || year > MaxSupportedYear)
        {
            throw new InvalidOperationException(
                $"Year must be between {MinSupportedYear} and {MaxSupportedYear}.");
        }
    }

    private static int CalculateRequestedDays(
        DateOnly startDate,
        DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new InvalidOperationException(
                "End date cannot be earlier than start date.");
        }

        return endDate.DayNumber
            - startDate.DayNumber
            + 1;
    }

    private static string NormalizeRequiredText(
        string value,
        string fieldName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{fieldName} cannot be empty.");
        }

        var normalizedValue =
            value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new InvalidOperationException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalizedValue;
    }
}
