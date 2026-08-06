namespace LeaveManagementSystem.IntegrationTests.Contracts;

public sealed record DepartmentLeaveStatisticsResponse(
    Guid DepartmentId,
    string DepartmentName,
    int ApprovedRequestCount,
    int TotalApprovedLeaveDays,
    decimal AverageApprovedLeaveDaysPerRequest);
