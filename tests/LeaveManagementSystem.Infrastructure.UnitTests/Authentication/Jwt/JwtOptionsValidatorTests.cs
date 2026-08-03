using LeaveManagementSystem.Infrastructure.Authentication.Jwt;
using Xunit;

namespace LeaveManagementSystem.Infrastructure.UnitTests.Authentication.Jwt;

public sealed class JwtOptionsValidatorTests
{
    private const string ValidSigningKey =
        "0123456789abcdef0123456789abcdef";

    private readonly JwtOptionsValidator _validator =
        new();

    [Fact]
    public void Validate_WithValidOptions_ReturnsSuccess()
    {
        var options =
            CreateValidOptions();

        var result =
            _validator.Validate(
                name: null,
                options);

        Assert.True(
            result.Succeeded);

        Assert.False(
            result.Failed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingIssuer_ReturnsFailure(
        string? invalidIssuer)
    {
        var options =
            CreateValidOptions();

        options.Issuer =
            invalidIssuer!;

        var result =
            _validator.Validate(
                name: null,
                options);

        Assert.True(
            result.Failed);

        Assert.Contains(
            "Jwt:Issuer is required.",
            result.Failures);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingAudience_ReturnsFailure(
        string? invalidAudience)
    {
        var options =
            CreateValidOptions();

        options.Audience =
            invalidAudience!;

        var result =
            _validator.Validate(
                name: null,
                options);

        Assert.True(
            result.Failed);

        Assert.Contains(
            "Jwt:Audience is required.",
            result.Failures);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingSigningKey_ReturnsFailure(
        string? invalidSigningKey)
    {
        var options =
            CreateValidOptions();

        options.SigningKey =
            invalidSigningKey!;

        var result =
            _validator.Validate(
                name: null,
                options);

        Assert.True(
            result.Failed);

        Assert.Contains(
            "Jwt:SigningKey is required.",
            result.Failures);
    }

    [Fact]
    public void Validate_WithSigningKeyShorterThanThirtyTwoBytes_ReturnsFailure()
    {
        var options =
            CreateValidOptions();

        options.SigningKey =
            "short-signing-key";

        var result =
            _validator.Validate(
                name: null,
                options);

        Assert.True(
            result.Failed);

        Assert.Contains(
            "Jwt:SigningKey must contain at least 32 bytes.",
            result.Failures);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1441)]
    public void Validate_WithExpirationOutsideAllowedRange_ReturnsFailure(
        int invalidExpirationMinutes)
    {
        var options =
            CreateValidOptions();

        options.AccessTokenExpirationMinutes =
            invalidExpirationMinutes;

        var result =
            _validator.Validate(
                name: null,
                options);

        Assert.True(
            result.Failed);

        Assert.Contains(
            "Jwt:AccessTokenExpirationMinutes must be between 1 and 1440.",
            result.Failures);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1440)]
    public void Validate_WithExpirationAtAllowedBoundary_ReturnsSuccess(
        int expirationMinutes)
    {
        var options =
            CreateValidOptions();

        options.AccessTokenExpirationMinutes =
            expirationMinutes;

        var result =
            _validator.Validate(
                name: null,
                options);

        Assert.True(
            result.Succeeded);
    }

    [Fact]
    public void Validate_WithMultipleInvalidValues_ReturnsAllFailures()
    {
        var options =
            new JwtOptions
            {
                Issuer = string.Empty,
                Audience = string.Empty,
                SigningKey = "short",
                AccessTokenExpirationMinutes = 0
            };

        var result =
            _validator.Validate(
                name: null,
                options);

        Assert.True(
            result.Failed);

        Assert.Equal(
            4,
            result.Failures.Count());

        Assert.Contains(
            "Jwt:Issuer is required.",
            result.Failures);

        Assert.Contains(
            "Jwt:Audience is required.",
            result.Failures);

        Assert.Contains(
            "Jwt:SigningKey must contain at least 32 bytes.",
            result.Failures);

        Assert.Contains(
            "Jwt:AccessTokenExpirationMinutes must be between 1 and 1440.",
            result.Failures);
    }

    [Fact]
    public void Validate_WithNullOptions_ThrowsArgumentNullException()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () => _validator.Validate(
                    name: null,
                    options: null!));

        Assert.Equal(
            "options",
            exception.ParamName);
    }

    private static JwtOptions CreateValidOptions()
    {
        return new JwtOptions
        {
            Issuer =
                "LeaveManagementSystem",

            Audience =
                "LeaveManagementSystem.Api",

            SigningKey =
                ValidSigningKey,

            AccessTokenExpirationMinutes =
                60
        };
    }
}
