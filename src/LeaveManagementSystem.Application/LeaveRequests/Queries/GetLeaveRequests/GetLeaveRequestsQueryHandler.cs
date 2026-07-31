using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequests;

public sealed class GetLeaveRequestsQueryHandler(
    ILeaveRequestReadRepository leaveRequestReadRepository)
    : IRequestHandler<
        GetLeaveRequestsQuery,
        IReadOnlyList<LeaveRequestDto>>
{
    public Task<IReadOnlyList<LeaveRequestDto>> Handle(
        GetLeaveRequestsQuery request,
        CancellationToken cancellationToken)
    {
        return leaveRequestReadRepository.GetAllAsync(
            cancellationToken);
    }
}
