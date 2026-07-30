namespace LeaveManagementSystem.IntegrationTests.Contracts;

internal sealed record ValidationProblemDetailsResponse(
    string Title,
    int Status,
    string? Instance,
    Dictionary<string, string[]> Errors,
    string TraceId);
