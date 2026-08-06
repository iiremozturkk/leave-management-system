using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Application.LeaveRequests.Dtos;

public sealed record LeaveCalendarItemDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeFullName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    int RequestedDays,
    LeaveRequestStatus Status);
