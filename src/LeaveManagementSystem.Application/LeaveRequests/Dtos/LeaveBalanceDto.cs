namespace LeaveManagementSystem.Application.LeaveRequests.Dtos;

public sealed record LeaveBalanceDto(
    Guid EmployeeId,
    Guid LeaveTypeId,
    string LeaveTypeName,
    int EntitledDays,
    int UsedDays,
    int RemainingDays);
