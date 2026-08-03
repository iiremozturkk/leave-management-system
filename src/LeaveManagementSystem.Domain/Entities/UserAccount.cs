using LeaveManagementSystem.Domain.Common;

namespace LeaveManagementSystem.Domain.Entities;

public sealed class UserAccount : BaseEntity
{
    private UserAccount()
    {
    }

    public UserAccount(
        Guid employeeId,
        string passwordHash)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Employee id cannot be empty.",
                nameof(employeeId));
        }

        EnsurePasswordHashIsValid(passwordHash);

        EmployeeId = employeeId;
        PasswordHash = passwordHash;
    }

    public Guid EmployeeId { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public string PasswordHash { get; private set; } =
        string.Empty;

    public bool IsActive { get; private set; } = true;

    public void ChangePasswordHash(
        string passwordHash)
    {
        EnsurePasswordHashIsValid(passwordHash);

        PasswordHash = passwordHash;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void EnsurePasswordHashIsValid(
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException(
                "Password hash cannot be empty.",
                nameof(passwordHash));
        }
    }
}
