using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Application.Authentication.Models;

public sealed record UserAccountAuthenticationData(
    Guid UserAccountId,
    Guid EmployeeId,
    string Email,
    EmployeeRole Role,
    bool IsUserAccountActive,
    bool IsEmployeeActive,
    string PasswordHash);
