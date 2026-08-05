using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.RejectLeaveRequest;

public sealed record RejectLeaveRequestCommand(
    Guid Id,
    string? ManagerComment)
    : IRequest<LeaveRequestDto?>;
