using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.IntegrationTests.Contracts;

public sealed record LeaveCalendarItemResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeFullName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    int RequestedDays,
    LeaveRequestStatus Status);
