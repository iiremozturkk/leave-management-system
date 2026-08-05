using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveBalance;

public sealed record GetLeaveBalanceQuery(
    Guid LeaveTypeId,
    int Year)
    : IRequest<LeaveBalanceDto?>;
