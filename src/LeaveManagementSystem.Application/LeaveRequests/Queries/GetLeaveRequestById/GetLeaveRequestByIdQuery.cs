using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequestById;

public sealed record GetLeaveRequestByIdQuery(
    Guid Id)
    : IRequest<LeaveRequestDto?>;
