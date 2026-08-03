using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Infrastructure.Authentication.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace LeaveManagementSystem.Infrastructure.UnitTests.Authentication.Security;

public sealed class PasswordHashServiceTests
{
    private const string Password =
        "Correct-Horse-Battery-Staple-123!";

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsNonPlaintextHash()
    {
        var service =
            CreateService();

        var passwordHash =
            service.HashPassword(Password);

        Assert.False(
            string.IsNullOrWhiteSpace(passwordHash));

        Assert.NotEqual(
            Password,
            passwordHash);
    }

    [Fact]
    public void HashPassword_CalledTwiceForSamePassword_ReturnsDifferentHashes()
    {
        var service =
            CreateService();

        var firstPasswordHash =
            service.HashPassword(Password);

        var secondPasswordHash =
            service.HashPassword(Password);

        Assert.NotEqual(
            firstPasswordHash,
            secondPasswordHash);

        Assert.Equal(
            PasswordVerificationOutcome.Succeeded,
            service.VerifyPassword(
                firstPasswordHash,
                Password));

        Assert.Equal(
            PasswordVerificationOutcome.Succeeded,
            service.VerifyPassword(
                secondPasswordHash,
                Password));
    }

    [Fact]
    public void HashPassword_WithNullPassword_ThrowsArgumentNullException()
    {
        var service =
            CreateService();

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => service.HashPassword(null!));

        Assert.Equal(
            "password",
            exception.ParamName);
    }

    [Fact]
    public void HashPassword_WithEmptyPassword_ThrowsArgumentException()
    {
        var service =
            CreateService();

        var exception =
            Assert.Throws<ArgumentException>(
                () => service.HashPassword(string.Empty));

        Assert.Equal(
            "password",
            exception.ParamName);
    }

    [Fact]
    public void VerifyPassword_WithMatchingPassword_ReturnsSucceeded()
    {
        var service =
            CreateService();

        var passwordHash =
            service.HashPassword(Password);

        var outcome =
            service.VerifyPassword(
                passwordHash,
                Password);

        Assert.Equal(
            PasswordVerificationOutcome.Succeeded,
            outcome);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFailed()
    {
        var service =
            CreateService();

        var passwordHash =
            service.HashPassword(Password);

        var outcome =
            service.VerifyPassword(
                passwordHash,
                "incorrect-password");

        Assert.Equal(
            PasswordVerificationOutcome.Failed,
            outcome);
    }

    [Fact]
    public void VerifyPassword_WithMalformedHash_ReturnsFailed()
    {
        var service =
            CreateService();

        var outcome =
            service.VerifyPassword(
                "not-a-valid-password-hash",
                Password);

        Assert.Equal(
            PasswordVerificationOutcome.Failed,
            outcome);
    }

    [Fact]
    public void VerifyPassword_WithIdentityV2Hash_ReturnsSucceededRehashNeeded()
    {
        var service =
            CreateService();

        var legacyHasher =
            new PasswordHasher<object>(
                Options.Create(
                    new PasswordHasherOptions
                    {
                        CompatibilityMode =
                            PasswordHasherCompatibilityMode.IdentityV2
                    }));

        var legacyHash =
            legacyHasher.HashPassword(
                new object(),
                Password);

        var outcome =
            service.VerifyPassword(
                legacyHash,
                Password);

        Assert.Equal(
            PasswordVerificationOutcome.SucceededRehashNeeded,
            outcome);
    }

    [Fact]
    public void VerifyPassword_WithNullHash_ThrowsArgumentNullException()
    {
        var service =
            CreateService();

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => service.VerifyPassword(
                    null!,
                    Password));

        Assert.Equal(
            "passwordHash",
            exception.ParamName);
    }

    [Fact]
    public void VerifyPassword_WithEmptyHash_ThrowsArgumentException()
    {
        var service =
            CreateService();

        var exception =
            Assert.Throws<ArgumentException>(
                () => service.VerifyPassword(
                    string.Empty,
                    Password));

        Assert.Equal(
            "passwordHash",
            exception.ParamName);
    }

    [Fact]
    public void VerifyPassword_WithNullProvidedPassword_ThrowsArgumentNullException()
    {
        var service =
            CreateService();

        var passwordHash =
            service.HashPassword(Password);

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => service.VerifyPassword(
                    passwordHash,
                    null!));

        Assert.Equal(
            "providedPassword",
            exception.ParamName);
    }

    [Fact]
    public void VerifyPassword_WithEmptyProvidedPassword_ThrowsArgumentException()
    {
        var service =
            CreateService();

        var passwordHash =
            service.HashPassword(Password);

        var exception =
            Assert.Throws<ArgumentException>(
                () => service.VerifyPassword(
                    passwordHash,
                    string.Empty));

        Assert.Equal(
            "providedPassword",
            exception.ParamName);
    }

    private static PasswordHashService CreateService()
    {
        return new PasswordHashService();
    }
}
