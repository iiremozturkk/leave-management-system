namespace LeaveManagementSystem.Application.LeaveRequests.Dtos;

public sealed record CreateLeaveRequestRequest(
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason);
