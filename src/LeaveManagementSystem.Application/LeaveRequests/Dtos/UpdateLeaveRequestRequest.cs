namespace LeaveManagementSystem.Application.LeaveRequests.Dtos;

public sealed record UpdateLeaveRequestRequest(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason);
