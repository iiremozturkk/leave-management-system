using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.ApproveLeaveRequest;

public sealed record ApproveLeaveRequestCommand(
    Guid Id,
    string? ManagerComment)
    : IRequest<LeaveRequestDto?>;
