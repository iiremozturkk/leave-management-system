namespace LeaveManagementSystem.Application.Reports.Dtos;

public sealed record DepartmentLeaveStatisticsDto(
    Guid DepartmentId,
    string DepartmentName,
    int ApprovedRequestCount,
    int TotalApprovedLeaveDays,
    decimal AverageApprovedLeaveDaysPerRequest);
