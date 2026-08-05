using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetMyLeaveRequests;

public sealed class GetMyLeaveRequestsQueryHandler(
    ILeaveRequestScopedReadRepository leaveRequestScopedReadRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IRequestHandler<
        GetMyLeaveRequestsQuery,
        IReadOnlyList<LeaveRequestDto>>
{
    public async Task<IReadOnlyList<LeaveRequestDto>> Handle(
        GetMyLeaveRequestsQuery request,
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
                "Only current active employees can access their leave requests.");
        }

        return await leaveRequestScopedReadRepository
            .GetAllForEmployeeAsync(
                currentUserAccess.EmployeeId,
                cancellationToken);
    }
}
