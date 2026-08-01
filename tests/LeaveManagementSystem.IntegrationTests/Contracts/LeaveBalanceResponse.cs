namespace LeaveManagementSystem.IntegrationTests.Contracts;

internal sealed record LeaveBalanceResponse(
    Guid EmployeeId,
    Guid LeaveTypeId,
    string LeaveTypeName,
    int Year,
    int EntitledDays,
    int UsedDays,
    int RemainingDays);
