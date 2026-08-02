using LeaveManagementSystem.Domain.Enums;
namespace LeaveManagementSystem.Application.LeaveRequests.Rules;

internal static class LeaveRequestRules
{
    private const int ReasonMaxLength = 500;
    private const int MinSupportedYear = 2000;
    private const int MaxSupportedYear = 2100;

    internal static void EnsureCanBeModified(
        LeaveRequestStatus status)
    {
        if (status != LeaveRequestStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending leave requests can be modified.");
        }
    }

    internal static string NormalizeReason(
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException(
                "Reason cannot be empty.");
        }

        var normalizedReason =
            reason.Trim();

        if (normalizedReason.Length > ReasonMaxLength)
        {
            throw new InvalidOperationException(
                $"Reason cannot exceed {ReasonMaxLength} characters.");
        }

        return normalizedReason;
    }

    internal static void EnsureSupportedDateRange(
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

    internal static IReadOnlyList<(int Year, int Days)>
        GetRequestedDaysByYear(
            DateOnly startDate,
            DateOnly endDate)
    {
        EnsureSupportedDateRange(
            startDate,
            endDate);

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

    internal static int CalculateEntitledDays(
        int defaultAnnualAllowanceDays,
        int year)
    {
        _ = year; // Reserved for future year-specific entitlement rules.

        return defaultAnnualAllowanceDays;
    }

    private static int CalculateDaysWithinYear(
        DateOnly startDate,
        DateOnly endDate,
        int year)
    {
        EnsureSupportedYear(
            year);

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
}
