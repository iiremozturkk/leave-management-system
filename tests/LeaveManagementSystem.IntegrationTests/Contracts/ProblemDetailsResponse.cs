namespace LeaveManagementSystem.IntegrationTests.Contracts;

internal sealed record ProblemDetailsResponse(
    string Title,
    string Detail,
    int Status,
    string? Instance,
    string TraceId);
