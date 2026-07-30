using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.IntegrationTests.Contracts;

internal sealed record LeaveRequestResponse(
    Guid Id,
    Guid EmployeeId,
    Guid LeaveTypeId,
    int RequestedDays,
    LeaveRequestStatus Status,
    string Reason);
