namespace LeaveManagementSystem.Application.LeaveRequests.Dtos;

public sealed record ReviewLeaveRequestRequest(
    Guid ReviewerEmployeeId,
    string? ManagerComment);
