using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.IntegrationTests.Contracts;

internal sealed record LeaveRequestResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeFullName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    int RequestedDays,
    LeaveRequestStatus Status,
    string Reason,
    string? ManagerComment,
    DateTime? ReviewedAtUtc,
    Guid? ReviewedByEmployeeId,
    string? ReviewedByEmployeeFullName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
