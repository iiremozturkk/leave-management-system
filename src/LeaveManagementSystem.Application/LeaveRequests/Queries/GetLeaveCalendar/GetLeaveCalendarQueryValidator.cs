using FluentValidation;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveCalendar;

public sealed class GetLeaveCalendarQueryValidator
    : AbstractValidator<GetLeaveCalendarQuery>
{
    public GetLeaveCalendarQueryValidator()
    {
        RuleFor(query => query.StartDate)
            .NotEqual(default(DateOnly))
            .WithMessage("Start date is required.");

        RuleFor(query => query.EndDate)
            .NotEqual(default(DateOnly))
            .WithMessage("End date is required.");

        RuleFor(query => query.EndDate)
            .GreaterThanOrEqualTo(query => query.StartDate)
            .When(query =>
                query.StartDate != default
                && query.EndDate != default)
            .WithMessage(
                "End date must be greater than or equal to start date.");
    }
}
