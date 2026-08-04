using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeaveManagementSystem.Application.Authentication.Constants;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.WebAPI.Authentication.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace LeaveManagementSystem.WebAPI.UnitTests.Authentication.Jwt;

public sealed class RequiredJwtBearerEventsTests
{
    [Fact]
    public void Constructor_WithNullClaimsValidator_ThrowsArgumentNullException()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new RequiredJwtBearerEvents(
                    null!));

        Assert.Equal(
            "claimsValidator",
            exception.ParamName);
    }

    [Fact]
    public async Task TokenValidated_WithValidRequiredClaims_DoesNotFailContext()
    {
        var events =
            new RequiredJwtBearerEvents(
                new RequiredJwtClaimsValidator());

        var context =
            CreateTokenValidatedContext(
                CreateValidPrincipal());

        await events.TokenValidated(
            context);

        Assert.Null(
            context.Result);
    }

    [Fact]
    public async Task TokenValidated_WithInvalidRequiredClaims_FailsContext()
    {
        var events =
            new RequiredJwtBearerEvents(
                new RequiredJwtClaimsValidator());

        var invalidPrincipal =
            CreateAuthenticatedPrincipal(
                new[]
                {
                    new Claim(
                        JwtRegisteredClaimNames.Sub,
                        Guid.NewGuid().ToString("D"))
                });

        var context =
            CreateTokenValidatedContext(
                invalidPrincipal);

        await events.TokenValidated(
            context);

        Assert.NotNull(
            context.Result);

        Assert.NotNull(
            context.Result.Failure);

        Assert.Equal(
            "The access token is invalid.",
            context.Result.Failure.Message);
    }

    [Fact]
    public async Task TokenValidated_WithNullPrincipal_FailsContext()
    {
        var events =
            new RequiredJwtBearerEvents(
                new RequiredJwtClaimsValidator());

        var context =
            CreateTokenValidatedContext(
                principal: null);

        await events.TokenValidated(
            context);

        Assert.NotNull(
            context.Result);

        Assert.NotNull(
            context.Result.Failure);

        Assert.Equal(
            "The access token is invalid.",
            context.Result.Failure.Message);
    }

    [Fact]
    public async Task TokenValidated_WithNullContext_ThrowsArgumentNullException()
    {
        var events =
            new RequiredJwtBearerEvents(
                new RequiredJwtClaimsValidator());

        var exception =
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => events.TokenValidated(
                    null!));

        Assert.Equal(
            "context",
            exception.ParamName);
    }

    private static TokenValidatedContext CreateTokenValidatedContext(
        ClaimsPrincipal? principal)
    {
        var authenticationScheme =
            new AuthenticationScheme(
                JwtBearerDefaults.AuthenticationScheme,
                displayName: null,
                typeof(JwtBearerHandler));

        return new TokenValidatedContext(
            new DefaultHttpContext(),
            authenticationScheme,
            new JwtBearerOptions())
        {
            Principal =
                principal
        };
    }

    private static ClaimsPrincipal CreateValidPrincipal()
    {
        return CreateAuthenticatedPrincipal(
            new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    Guid.NewGuid().ToString("D")),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString("D")),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    "jwt.events@example.com"),

                new Claim(
                    JwtClaimNames.EmployeeId,
                    Guid.NewGuid().ToString("D")),

                new Claim(
                    JwtClaimNames.Role,
                    EmployeeRole.Manager.ToString())
            });
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(
        IEnumerable<Claim> claims)
    {
        var identity =
            new ClaimsIdentity(
                claims,
                "TestAuthentication");

        return new ClaimsPrincipal(
            identity);
    }
}
