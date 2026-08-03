namespace LeaveManagementSystem.Application.Authentication.Abstractions;

public enum PasswordVerificationOutcome
{
    Failed = 0,
    Succeeded = 1,
    SucceededRehashNeeded = 2
}
