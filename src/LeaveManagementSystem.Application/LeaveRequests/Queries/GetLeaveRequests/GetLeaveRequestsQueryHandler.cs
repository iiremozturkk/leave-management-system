using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Domain.Enums;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequests;

public sealed class GetLeaveRequestsQueryHandler(
    ILeaveRequestReadRepository leaveRequestReadRepository,
    ILeaveRequestScopedReadRepository leaveRequestScopedReadRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IRequestHandler<
        GetLeaveRequestsQuery,
        IReadOnlyList<LeaveRequestDto>>
{
    public async Task<IReadOnlyList<LeaveRequestDto>> Handle(
        GetLeaveRequestsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        var currentUserAccess =
            await currentUserAccessService.GetAsync(
                cancellationToken);

        if (currentUserAccess is null)
        {
            throw new ForbiddenOperationException(
                "Only current active employees can access leave requests.");
        }

        return currentUserAccess.Role switch
        {
            EmployeeRole.Employee =>
                await leaveRequestScopedReadRepository
                    .GetAllForEmployeeAsync(
                        currentUserAccess.EmployeeId,
                        cancellationToken),

            EmployeeRole.Manager =>
                await leaveRequestScopedReadRepository
                    .GetAllForManagerAsync(
                        currentUserAccess.EmployeeId,
                        cancellationToken),

            EmployeeRole.HR =>
                await leaveRequestReadRepository
                    .GetAllAsync(
                        cancellationToken),

            _ =>
                throw new ForbiddenOperationException(
                    "The current employee role is not authorized to access leave requests.")
        };
    }
}
