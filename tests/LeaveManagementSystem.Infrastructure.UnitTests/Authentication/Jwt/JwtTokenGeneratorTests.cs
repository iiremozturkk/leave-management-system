using System.IdentityModel.Tokens.Jwt;
using System.Text;
using LeaveManagementSystem.Application.Authentication.Constants;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Authentication.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LeaveManagementSystem.Infrastructure.UnitTests.Authentication.Jwt;

public sealed class JwtTokenGeneratorTests
{
    private const string Issuer =
        "LeaveManagementSystem";

    private const string Audience =
        "LeaveManagementSystem.Api";

    private const string SigningKey =
        "0123456789abcdef0123456789abcdef";

    private const int ExpirationMinutes =
        60;

    private static readonly DateTimeOffset FixedUtcNow =
        new(
            2026,
            8,
            3,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void GenerateToken_WithValidRequest_ReturnsSignedTokenWithExpectedClaims()
    {
        var generator =
            CreateGenerator();

        var request =
            CreateValidRequest();

        var result =
            generator.GenerateToken(request);

        Assert.False(
            string.IsNullOrWhiteSpace(result.AccessToken));

        Assert.Equal(
            FixedUtcNow.UtcDateTime.AddMinutes(
                ExpirationMinutes),
            result.ExpiresAtUtc);

        var tokenHandler =
            new JwtSecurityTokenHandler();

        var token =
            tokenHandler.ReadJwtToken(
                result.AccessToken);

        Assert.Equal(
            Issuer,
            token.Issuer);

        Assert.Equal(
            Audience,
            Assert.Single(token.Audiences));

        Assert.Equal(
            FixedUtcNow.UtcDateTime,
            token.IssuedAt);

        Assert.Equal(
            FixedUtcNow.UtcDateTime,
            token.ValidFrom);

        Assert.Equal(
            result.ExpiresAtUtc,
            token.ValidTo);

        Assert.Equal(
            SecurityAlgorithms.HmacSha256,
            token.Header.Alg);

        Assert.Equal(
            request.UserAccountId.ToString("D"),
            GetClaimValue(
                token,
                JwtRegisteredClaimNames.Sub));

        Assert.Equal(
            request.EmployeeId.ToString("D"),
            GetClaimValue(
                token,
                JwtClaimNames.EmployeeId));

        Assert.Equal(
            request.Email,
            GetClaimValue(
                token,
                JwtRegisteredClaimNames.Email));

        Assert.Equal(
            request.Role.ToString(),
            GetClaimValue(
                token,
                JwtClaimNames.Role));

        var jti =
            GetClaimValue(
                token,
                JwtRegisteredClaimNames.Jti);

        Assert.True(
            Guid.TryParse(
                jti,
                out _));

        var validationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,

                ValidateAudience = true,
                ValidAudience = Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            SigningKey)),

                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            };

        var principal =
            tokenHandler.ValidateToken(
                result.AccessToken,
                validationParameters,
                out var validatedToken);

        Assert.NotNull(
            principal);

        Assert.IsType<JwtSecurityToken>(
            validatedToken);
    }

    [Fact]
    public void GenerateToken_CalledTwice_ReturnsDifferentTokenIdentifiers()
    {
        var generator =
            CreateGenerator();

        var request =
            CreateValidRequest();

        var firstResult =
            generator.GenerateToken(request);

        var secondResult =
            generator.GenerateToken(request);

        Assert.NotEqual(
            firstResult.AccessToken,
            secondResult.AccessToken);

        var tokenHandler =
            new JwtSecurityTokenHandler();

        var firstToken =
            tokenHandler.ReadJwtToken(
                firstResult.AccessToken);

        var secondToken =
            tokenHandler.ReadJwtToken(
                secondResult.AccessToken);

        Assert.NotEqual(
            GetClaimValue(
                firstToken,
                JwtRegisteredClaimNames.Jti),
            GetClaimValue(
                secondToken,
                JwtRegisteredClaimNames.Jti));
    }

    [Fact]
    public void GenerateToken_WithNullRequest_ThrowsArgumentNullException()
    {
        var generator =
            CreateGenerator();

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => generator.GenerateToken(
                    null!));

        Assert.Equal(
            "request",
            exception.ParamName);
    }

    [Fact]
    public void GenerateToken_WithEmptyUserAccountId_ThrowsArgumentException()
    {
        var generator =
            CreateGenerator();

        var request =
            CreateValidRequest() with
            {
                UserAccountId = Guid.Empty
            };

        var exception =
            Assert.Throws<ArgumentException>(
                () => generator.GenerateToken(
                    request));

        Assert.Equal(
            "UserAccountId",
            exception.ParamName);

        Assert.StartsWith(
            "User account id cannot be empty.",
            exception.Message);
    }

    [Fact]
    public void GenerateToken_WithEmptyEmployeeId_ThrowsArgumentException()
    {
        var generator =
            CreateGenerator();

        var request =
            CreateValidRequest() with
            {
                EmployeeId = Guid.Empty
            };

        var exception =
            Assert.Throws<ArgumentException>(
                () => generator.GenerateToken(
                    request));

        Assert.Equal(
            "EmployeeId",
            exception.ParamName);

        Assert.StartsWith(
            "Employee id cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateToken_WithInvalidEmail_ThrowsArgumentException(
        string? invalidEmail)
    {
        var generator =
            CreateGenerator();

        var request =
            CreateValidRequest() with
            {
                Email = invalidEmail!
            };

        var exception =
            Assert.Throws<ArgumentException>(
                () => generator.GenerateToken(
                    request));

        Assert.Equal(
            "Email",
            exception.ParamName);

        Assert.StartsWith(
            "Email cannot be empty.",
            exception.Message);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new JwtTokenGenerator(
                    null!,
                    TimeProvider.System));

        Assert.Equal(
            "jwtOptions",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new JwtTokenGenerator(
                    CreateOptions(),
                    null!));

        Assert.Equal(
            "timeProvider",
            exception.ParamName);
    }

    [Fact]
    public void GenerateToken_WithUnsupportedRole_ThrowsArgumentOutOfRangeException()
    {
        var generator =
            CreateGenerator();

        var invalidRole =
            (EmployeeRole)999;

        var request =
            CreateValidRequest() with
            {
                Role = invalidRole
            };

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => generator.GenerateToken(
                    request));

        Assert.Equal(
            "Role",
            exception.ParamName);

        Assert.Equal(
            invalidRole,
            (EmployeeRole)exception.ActualValue!);
    }

    private static JwtTokenGenerator CreateGenerator()
    {
        return new JwtTokenGenerator(
            CreateOptions(),
            new FixedTimeProvider(
                FixedUtcNow));
    }

    private static IOptions<JwtOptions> CreateOptions()
    {
        return Options.Create(
            new JwtOptions
            {
                Issuer = Issuer,
                Audience = Audience,
                SigningKey = SigningKey,
                AccessTokenExpirationMinutes =
                    ExpirationMinutes
            });
    }

    private static JwtTokenRequest CreateValidRequest()
    {
        return new JwtTokenRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "employee@example.com",
            EmployeeRole.Manager);
    }

    private static string GetClaimValue(
        JwtSecurityToken token,
        string claimType)
    {
        return token.Claims
            .Single(
                claim =>
                    claim.Type == claimType)
            .Value;
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
