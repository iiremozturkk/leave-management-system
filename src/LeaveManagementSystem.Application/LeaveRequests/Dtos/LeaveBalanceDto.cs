namespace LeaveManagementSystem.Application.LeaveRequests.Dtos;

public sealed record LeaveBalanceDto(
    Guid EmployeeId,
    Guid LeaveTypeId,
    string LeaveTypeName,
    int Year,
    int EntitledDays,
    int UsedDays,
    int RemainingDays);