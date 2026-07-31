using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequestById;

public sealed class GetLeaveRequestByIdQueryHandler(
    ILeaveRequestReadRepository leaveRequestReadRepository)
    : IRequestHandler<
        GetLeaveRequestByIdQuery,
        LeaveRequestDto?>
{
    public Task<LeaveRequestDto?> Handle(
        GetLeaveRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        return leaveRequestReadRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
