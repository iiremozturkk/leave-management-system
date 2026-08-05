using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequestById;

public sealed class GetLeaveRequestByIdQueryHandler(
    ILeaveRequestSelfServiceReadRepository
        leaveRequestSelfServiceReadRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IRequestHandler<
        GetLeaveRequestByIdQuery,
        LeaveRequestDto?>
{
    public async Task<LeaveRequestDto?> Handle(
        GetLeaveRequestByIdQuery request,
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
                "Only current active employees can use leave self-service operations.");
        }

        return await leaveRequestSelfServiceReadRepository
            .GetByIdForEmployeeAsync(
                request.Id,
                currentUserAccess.EmployeeId,
                cancellationToken);
    }
}
