using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Application.Authentication.Models;

public sealed record CurrentUserAccessData(
    Guid UserAccountId,
    Guid EmployeeId,
    string Email,
    EmployeeRole Role,
    bool IsUserAccountActive,
    bool IsEmployeeActive);
