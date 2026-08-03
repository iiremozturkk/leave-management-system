namespace LeaveManagementSystem.Application.Authentication.Abstractions;

public interface IPasswordHashService
{
    string HashPassword(
        string password);

    PasswordVerificationOutcome VerifyPassword(
        string passwordHash,
        string providedPassword);
}
