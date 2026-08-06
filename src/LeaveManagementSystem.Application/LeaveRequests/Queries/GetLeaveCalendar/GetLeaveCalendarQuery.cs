using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveCalendar;

public sealed record GetLeaveCalendarQuery(
    DateOnly StartDate,
    DateOnly EndDate)
    : IRequest<IReadOnlyList<LeaveCalendarItemDto>>;
