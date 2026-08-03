using LeaveManagementSystem.Application.Authentication.Commands.Login;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.Authentication.Login;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator =
        new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsSuccess()
    {
        var command =
            CreateValidCommand();

        var result =
            _validator.Validate(command);

        Assert.True(
            result.IsValid);

        Assert.Empty(
            result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingEmail_ReturnsRequiredError(
        string? invalidEmail)
    {
        var command =
            CreateValidCommand() with
            {
                Email = invalidEmail!
            };

        var result =
            _validator.Validate(command);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            nameof(LoginCommand.Email),
            error.PropertyName);

        Assert.Equal(
            "Email is required.",
            error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithEmailLongerThanMaximum_ReturnsMaximumLengthError()
    {
        var command =
            CreateValidCommand() with
            {
                Email = new string('a', 256)
            };

        var result =
            _validator.Validate(command);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            nameof(LoginCommand.Email),
            error.PropertyName);

        Assert.Equal(
            "Email must not exceed 255 characters.",
            error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_ReturnsEmailAddressError()
    {
        var command =
            CreateValidCommand() with
            {
                Email = "not-an-email"
            };

        var result =
            _validator.Validate(command);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            nameof(LoginCommand.Email),
            error.PropertyName);

        Assert.Equal(
            "Email must be a valid email address.",
            error.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingPassword_ReturnsRequiredError(
        string? invalidPassword)
    {
        var command =
            CreateValidCommand() with
            {
                Password = invalidPassword!
            };

        var result =
            _validator.Validate(command);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            nameof(LoginCommand.Password),
            error.PropertyName);

        Assert.Equal(
            "Password is required.",
            error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithPasswordLongerThanMaximum_ReturnsMaximumLengthError()
    {
        var command =
            CreateValidCommand() with
            {
                Password = new string('a', 257)
            };

        var result =
            _validator.Validate(command);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            nameof(LoginCommand.Password),
            error.PropertyName);

        Assert.Equal(
            "Password must not exceed 256 characters.",
            error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithPasswordAtMaximumLength_ReturnsSuccess()
    {
        var command =
            CreateValidCommand() with
            {
                Password = new string('a', 256)
            };

        var result =
            _validator.Validate(command);

        Assert.True(
            result.IsValid);

        Assert.Empty(
            result.Errors);
    }

    private static LoginCommand CreateValidCommand()
    {
        return new LoginCommand(
            "employee@example.com",
            "Correct-Horse-Battery-Staple-123!");
    }
}
