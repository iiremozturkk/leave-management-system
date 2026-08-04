using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Application.Authentication.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserAccountId { get; }

    Guid? EmployeeId { get; }

    string? Email { get; }

    EmployeeRole? Role { get; }
}
