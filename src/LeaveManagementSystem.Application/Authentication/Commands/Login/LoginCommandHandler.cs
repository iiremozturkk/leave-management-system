using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;
using MediatR;

namespace LeaveManagementSystem.Application.Authentication.Commands.Login;

public sealed class LoginCommandHandler(
    IUserAccountReadRepository userAccountReadRepository,
    IUserAccountWriteRepository userAccountWriteRepository,
    IPasswordHashService passwordHashService,
    IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<LoginCommand, LoginResult?>
{
    public async Task<LoginResult?> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedEmail =
            request.Email
                .Trim()
                .ToLowerInvariant();

        var authenticationData =
            await userAccountReadRepository.GetByEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (authenticationData is null)
        {
            return null;
        }

        var verificationOutcome =
            passwordHashService.VerifyPassword(
                authenticationData.PasswordHash,
                request.Password);

        if (verificationOutcome ==
            PasswordVerificationOutcome.Failed)
        {
            return null;
        }

        if (!authenticationData.IsUserAccountActive
            || !authenticationData.IsEmployeeActive)
        {
            return null;
        }

        if (verificationOutcome ==
            PasswordVerificationOutcome.SucceededRehashNeeded)
        {
            var userAccount =
                await userAccountWriteRepository.GetForUpdateAsync(
                    authenticationData.UserAccountId,
                    cancellationToken);

            if (userAccount is null
                || !userAccount.IsActive)
            {
                return null;
            }

            if (!string.Equals(
                    userAccount.PasswordHash,
                    authenticationData.PasswordHash,
                    StringComparison.Ordinal))
            {
                return null;
            }

            var newPasswordHash =
                passwordHashService.HashPassword(
                    request.Password);

            userAccount.ChangePasswordHash(
                newPasswordHash);

            await userAccountWriteRepository.SaveChangesAsync(
                cancellationToken);
        }
        else if (verificationOutcome !=
                 PasswordVerificationOutcome.Succeeded)
        {
            throw new InvalidOperationException(
                "Unsupported password verification outcome.");
        }

        var tokenResult =
            jwtTokenGenerator.GenerateToken(
                new JwtTokenRequest(
                    authenticationData.UserAccountId,
                    authenticationData.EmployeeId,
                    authenticationData.Email,
                    authenticationData.Role));

        return new LoginResult(
            tokenResult.AccessToken,
            tokenResult.ExpiresAtUtc,
            authenticationData.UserAccountId,
            authenticationData.EmployeeId,
            authenticationData.Email,
            authenticationData.Role);
    }
}
