using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.ApproveLeaveRequest;

public sealed record ApproveLeaveRequestCommand(
    Guid Id,
    Guid ReviewerEmployeeId,
    string? ManagerComment)
    : IRequest<LeaveRequestDto?>;
