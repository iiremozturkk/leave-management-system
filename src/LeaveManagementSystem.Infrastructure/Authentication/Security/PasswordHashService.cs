using LeaveManagementSystem.Application.Authentication.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace LeaveManagementSystem.Infrastructure.Authentication.Security;

public sealed class PasswordHashService
    : IPasswordHashService
{
    private static readonly object HashingContext = new();

    private readonly PasswordHasher<object> _passwordHasher = new();

    public string HashPassword(
        string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        return _passwordHasher.HashPassword(
            HashingContext,
            password);
    }

    public PasswordVerificationOutcome VerifyPassword(
        string passwordHash,
        string providedPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(passwordHash);
        ArgumentException.ThrowIfNullOrEmpty(providedPassword);

        try
        {
            var verificationResult =
                _passwordHasher.VerifyHashedPassword(
                    HashingContext,
                    passwordHash,
                    providedPassword);

            return verificationResult switch
            {
                PasswordVerificationResult.Failed =>
                    PasswordVerificationOutcome.Failed,

                PasswordVerificationResult.Success =>
                    PasswordVerificationOutcome.Succeeded,

                PasswordVerificationResult.SuccessRehashNeeded =>
                    PasswordVerificationOutcome.SucceededRehashNeeded,

                _ => throw new InvalidOperationException(
                    "Unsupported password verification result.")
            };
        }
        catch (FormatException)
        {
            return PasswordVerificationOutcome.Failed;
        }
    }
}
