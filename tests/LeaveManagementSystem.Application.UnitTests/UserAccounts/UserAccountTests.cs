using LeaveManagementSystem.Domain.Entities;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.UserAccounts;

public sealed class UserAccountTests
{
    private const string InitialPasswordHash =
        "initial-password-hash";

    [Fact]
    public void Constructor_WithValidValues_CreatesActiveAccount()
    {
        var employeeId =
            Guid.NewGuid();

        var beforeCreationUtc =
            DateTime.UtcNow;

        var userAccount =
            new UserAccount(
                employeeId,
                InitialPasswordHash);

        var afterCreationUtc =
            DateTime.UtcNow;

        Assert.NotEqual(
            Guid.Empty,
            userAccount.Id);

        Assert.Equal(
            employeeId,
            userAccount.EmployeeId);

        Assert.Equal(
            InitialPasswordHash,
            userAccount.PasswordHash);

        Assert.True(
            userAccount.IsActive);

        Assert.InRange(
            userAccount.CreatedAtUtc,
            beforeCreationUtc,
            afterCreationUtc);

        Assert.Null(
            userAccount.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptyEmployeeId_ThrowsArgumentException()
    {
        var exception =
            Assert.Throws<ArgumentException>(
                () => new UserAccount(
                    Guid.Empty,
                    InitialPasswordHash));

        Assert.Equal(
            "employeeId",
            exception.ParamName);

        Assert.StartsWith(
            "Employee id cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidPasswordHash_ThrowsArgumentException(
        string? invalidPasswordHash)
    {
        var exception =
            Assert.Throws<ArgumentException>(
                () => new UserAccount(
                    Guid.NewGuid(),
                    invalidPasswordHash!));

        Assert.Equal(
            "passwordHash",
            exception.ParamName);

        Assert.StartsWith(
            "Password hash cannot be empty.",
            exception.Message);
    }

    [Fact]
    public void ChangePasswordHash_WithNewHash_UpdatesHashAndTimestamp()
    {
        var userAccount =
            CreateUserAccount();

        var beforeChangeUtc =
            DateTime.UtcNow;

        userAccount.ChangePasswordHash(
            "new-password-hash");

        var afterChangeUtc =
            DateTime.UtcNow;

        Assert.Equal(
            "new-password-hash",
            userAccount.PasswordHash);

        Assert.NotNull(
            userAccount.UpdatedAtUtc);

        Assert.InRange(
            userAccount.UpdatedAtUtc.Value,
            beforeChangeUtc,
            afterChangeUtc);
    }

    [Fact]
    public void ChangePasswordHash_WithSameHash_DoesNotUpdateTimestamp()
    {
        var userAccount =
            CreateUserAccount();

        userAccount.ChangePasswordHash(
            InitialPasswordHash);

        Assert.Equal(
            InitialPasswordHash,
            userAccount.PasswordHash);

        Assert.Null(
            userAccount.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangePasswordHash_WithInvalidHash_DoesNotMutateAccount(
        string? invalidPasswordHash)
    {
        var userAccount =
            CreateUserAccount();

        userAccount.ChangePasswordHash(
            "current-password-hash");

        var previousUpdatedAtUtc =
            userAccount.UpdatedAtUtc;

        var exception =
            Assert.Throws<ArgumentException>(
                () => userAccount.ChangePasswordHash(
                    invalidPasswordHash!));

        Assert.Equal(
            "passwordHash",
            exception.ParamName);

        Assert.StartsWith(
            "Password hash cannot be empty.",
            exception.Message);

        Assert.Equal(
            "current-password-hash",
            userAccount.PasswordHash);

        Assert.Equal(
            previousUpdatedAtUtc,
            userAccount.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_WhenActive_DeactivatesAccountAndUpdatesTimestamp()
    {
        var userAccount =
            CreateUserAccount();

        var beforeChangeUtc =
            DateTime.UtcNow;

        userAccount.Deactivate();

        var afterChangeUtc =
            DateTime.UtcNow;

        Assert.False(
            userAccount.IsActive);

        Assert.NotNull(
            userAccount.UpdatedAtUtc);

        Assert.InRange(
            userAccount.UpdatedAtUtc.Value,
            beforeChangeUtc,
            afterChangeUtc);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_DoesNotChangeTimestamp()
    {
        var userAccount =
            CreateUserAccount();

        userAccount.Deactivate();

        var previousUpdatedAtUtc =
            userAccount.UpdatedAtUtc;

        userAccount.Deactivate();

        Assert.False(
            userAccount.IsActive);

        Assert.Equal(
            previousUpdatedAtUtc,
            userAccount.UpdatedAtUtc);
    }

    [Fact]
    public void Activate_WhenInactive_ActivatesAccountAndUpdatesTimestamp()
    {
        var userAccount =
            CreateUserAccount();

        userAccount.Deactivate();

        var beforeChangeUtc =
            DateTime.UtcNow;

        userAccount.Activate();

        var afterChangeUtc =
            DateTime.UtcNow;

        Assert.True(
            userAccount.IsActive);

        Assert.NotNull(
            userAccount.UpdatedAtUtc);

        Assert.InRange(
            userAccount.UpdatedAtUtc.Value,
            beforeChangeUtc,
            afterChangeUtc);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_DoesNotUpdateTimestamp()
    {
        var userAccount =
            CreateUserAccount();

        userAccount.Activate();

        Assert.True(
            userAccount.IsActive);

        Assert.Null(
            userAccount.UpdatedAtUtc);
    }

    private static UserAccount CreateUserAccount()
    {
        return new UserAccount(
            Guid.NewGuid(),
            InitialPasswordHash);
    }
}
