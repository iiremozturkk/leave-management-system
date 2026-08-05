namespace LeaveManagementSystem.Application.LeaveRequests.Dtos;

public sealed record CreateLeaveRequestRequest(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason);
