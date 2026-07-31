using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequests;

public sealed record GetLeaveRequestsQuery
    : IRequest<IReadOnlyList<LeaveRequestDto>>;
