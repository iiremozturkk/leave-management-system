using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.RejectLeaveRequest;

public sealed record RejectLeaveRequestCommand(
    Guid Id,
    Guid ReviewerEmployeeId,
    string? ManagerComment)
    : IRequest<LeaveRequestDto?>;
