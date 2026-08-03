namespace LeaveManagementSystem.IntegrationTests.Contracts;

public sealed record ValidationProblemDetailsResponse(
    int? Status,
    string? Title,
    string? Detail,
    string? Instance,
    string? TraceId,
    Dictionary<string, string[]> Errors);
