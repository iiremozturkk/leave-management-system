using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Application.Authentication.Models;

public sealed record CurrentUserAccess(
    Guid UserAccountId,
    Guid EmployeeId,
    string Email,
    EmployeeRole Role);
