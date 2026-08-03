using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Application.Authentication.Models;

public sealed record LoginResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserAccountId,
    Guid EmployeeId,
    string Email,
    EmployeeRole Role);
