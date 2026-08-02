using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.DeleteLeaveRequest;

public sealed record DeleteLeaveRequestCommand(
    Guid Id)
    : IRequest<bool>;
