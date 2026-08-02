using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.UpdateLeaveRequest;

public sealed record UpdateLeaveRequestCommand(
    Guid Id,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason)
    : IRequest<LeaveRequestDto?>;
