using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Domain.Enums;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveCalendar;

public sealed class GetLeaveCalendarQueryHandler(
    ILeaveCalendarReadRepository leaveCalendarReadRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IRequestHandler<
        GetLeaveCalendarQuery,
        IReadOnlyList<LeaveCalendarItemDto>>
{
    public async Task<IReadOnlyList<LeaveCalendarItemDto>> Handle(
        GetLeaveCalendarQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentUserAccess =
            await currentUserAccessService.GetAsync(
                cancellationToken);

        if (currentUserAccess is null)
        {
            throw new ForbiddenOperationException(
                "Only current active employees can access the leave calendar.");
        }

        return currentUserAccess.Role switch
        {
            EmployeeRole.Employee =>
                await leaveCalendarReadRepository
                    .GetCalendarForEmployeeAsync(
                        currentUserAccess.EmployeeId,
                        request.StartDate,
                        request.EndDate,
                        cancellationToken),

            EmployeeRole.Manager =>
                await leaveCalendarReadRepository
                    .GetCalendarForManagerAsync(
                        currentUserAccess.EmployeeId,
                        request.StartDate,
                        request.EndDate,
                        cancellationToken),

            EmployeeRole.HR =>
                await leaveCalendarReadRepository
                    .GetCalendarAsync(
                        request.StartDate,
                        request.EndDate,
                        cancellationToken),

            _ =>
                throw new ForbiddenOperationException(
                    "The current employee role is not authorized to access the leave calendar.")
        };
    }
}
