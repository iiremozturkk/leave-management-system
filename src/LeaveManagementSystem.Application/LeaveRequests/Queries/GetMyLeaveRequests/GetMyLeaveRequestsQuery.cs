using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetMyLeaveRequests;

public sealed record GetMyLeaveRequestsQuery
    : IRequest<IReadOnlyList<LeaveRequestDto>>;
