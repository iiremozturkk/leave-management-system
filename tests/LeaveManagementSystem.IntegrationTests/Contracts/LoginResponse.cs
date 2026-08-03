using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.IntegrationTests.Contracts;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserAccountId,
    Guid EmployeeId,
    string Email,
    EmployeeRole Role);
