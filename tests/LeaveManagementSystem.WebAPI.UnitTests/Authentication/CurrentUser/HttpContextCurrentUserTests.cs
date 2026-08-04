using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeaveManagementSystem.Application.Authentication.Constants;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.WebAPI.Authentication.CurrentUser;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace LeaveManagementSystem.WebAPI.UnitTests.Authentication.CurrentUser;

public sealed class HttpContextCurrentUserTests
{
    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ThrowsArgumentNullException()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new HttpContextCurrentUser(
                    null!));

        Assert.Equal(
            "httpContextAccessor",
            exception.ParamName);
    }

    [Fact]
    public void Properties_WithoutHttpContext_ReturnUnauthenticatedAndNullValues()
    {
        var httpContextAccessor =
            new HttpContextAccessor();

        var currentUser =
            new HttpContextCurrentUser(
                httpContextAccessor);

        Assert.False(
            currentUser.IsAuthenticated);

        Assert.Null(
            currentUser.UserAccountId);

        Assert.Null(
            currentUser.EmployeeId);

        Assert.Null(
            currentUser.Email);

        Assert.Null(
            currentUser.Role);
    }

    [Fact]
    public void Properties_WithValidAuthenticatedIdentity_ReturnExpectedValues()
    {
        var userAccountId =
            Guid.NewGuid();

        var employeeId =
            Guid.NewGuid();

        const string email =
            "current.user@example.com";

        var identity =
            CreateIdentity(
                new[]
                {
                    new Claim(
                        JwtRegisteredClaimNames.Sub,
                        userAccountId.ToString("D")),

                    new Claim(
                        JwtClaimNames.EmployeeId,
                        employeeId.ToString("D")),

                    new Claim(
                        JwtRegisteredClaimNames.Email,
                        email),

                    new Claim(
                        JwtClaimNames.Role,
                        EmployeeRole.Manager.ToString())
                });

        var currentUser =
            CreateCurrentUser(
                identity);

        Assert.True(
            currentUser.IsAuthenticated);

        Assert.Equal(
            userAccountId,
            currentUser.UserAccountId);

        Assert.Equal(
            employeeId,
            currentUser.EmployeeId);

        Assert.Equal(
            email,
            currentUser.Email);

        Assert.Equal(
            EmployeeRole.Manager,
            currentUser.Role);
    }

    [Fact]
    public void Properties_WithOnlyUnauthenticatedIdentity_ReturnUnauthenticatedAndNullValues()
    {
        var identity =
            CreateIdentity(
                new[]
                {
                    new Claim(
                        JwtRegisteredClaimNames.Sub,
                        Guid.NewGuid().ToString("D")),

                    new Claim(
                        JwtClaimNames.EmployeeId,
                        Guid.NewGuid().ToString("D")),

                    new Claim(
                        JwtRegisteredClaimNames.Email,
                        "unauthenticated@example.com"),

                    new Claim(
                        JwtClaimNames.Role,
                        EmployeeRole.Employee.ToString())
                },
                isAuthenticated: false);

        var currentUser =
            CreateCurrentUser(
                identity);

        Assert.False(
            currentUser.IsAuthenticated);

        Assert.Null(
            currentUser.UserAccountId);

        Assert.Null(
            currentUser.EmployeeId);

        Assert.Null(
            currentUser.Email);

        Assert.Null(
            currentUser.Role);
    }

    [Fact]
    public void Properties_WithMultipleAuthenticatedIdentities_ReturnUnauthenticatedAndNullValues()
    {
        var firstIdentity =
            CreateIdentity(
                new[]
                {
                    new Claim(
                        JwtRegisteredClaimNames.Sub,
                        Guid.NewGuid().ToString("D"))
                });

        var secondIdentity =
            CreateIdentity(
                new[]
                {
                    new Claim(
                        JwtClaimNames.EmployeeId,
                        Guid.NewGuid().ToString("D"))
                });

        var currentUser =
            CreateCurrentUser(
                firstIdentity,
                secondIdentity);

        Assert.False(
            currentUser.IsAuthenticated);

        Assert.Null(
            currentUser.UserAccountId);

        Assert.Null(
            currentUser.EmployeeId);

        Assert.Null(
            currentUser.Email);

        Assert.Null(
            currentUser.Role);
    }

    [Fact]
    public void Properties_DoNotReadClaimsFromUnauthenticatedIdentity()
    {
        var authenticatedIdentity =
            CreateIdentity(
                Array.Empty<Claim>());

        var unauthenticatedIdentity =
            CreateIdentity(
                new[]
                {
                    new Claim(
                        JwtRegisteredClaimNames.Sub,
                        Guid.NewGuid().ToString("D")),

                    new Claim(
                        JwtClaimNames.EmployeeId,
                        Guid.NewGuid().ToString("D")),

                    new Claim(
                        JwtRegisteredClaimNames.Email,
                        "ignored@example.com"),

                    new Claim(
                        JwtClaimNames.Role,
                        EmployeeRole.HR.ToString())
                },
                isAuthenticated: false);

        var currentUser =
            CreateCurrentUser(
                authenticatedIdentity,
                unauthenticatedIdentity);

        Assert.True(
            currentUser.IsAuthenticated);

        Assert.Null(
            currentUser.UserAccountId);

        Assert.Null(
            currentUser.EmployeeId);

        Assert.Null(
            currentUser.Email);

        Assert.Null(
            currentUser.Role);
    }

    [Fact]
    public void UserAccountId_WithDuplicateSubClaims_ReturnsNull()
    {
        var duplicateValue =
            Guid.NewGuid()
                .ToString("D");

        var identity =
            CreateIdentity(
                new[]
                {
                    new Claim(
                        JwtRegisteredClaimNames.Sub,
                        duplicateValue),

                    new Claim(
                        JwtRegisteredClaimNames.Sub,
                        duplicateValue)
                });

        var currentUser =
            CreateCurrentUser(
                identity);

        Assert.True(
            currentUser.IsAuthenticated);

        Assert.Null(
            currentUser.UserAccountId);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void UserAccountId_WithInvalidGuidValue_ReturnsNull(
        string claimValue)
    {
        var identity =
            CreateIdentity(
                new[]
                {
                    new Claim(
                        JwtRegisteredClaimNames.Sub,
                        claimValue)
                });

        var currentUser =
            CreateCurrentUser(
                identity);

        Assert.True(
            currentUser.IsAuthenticated);

        Assert.Null(
            currentUser.UserAccountId);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void EmployeeId_WithInvalidGuidValue_ReturnsNull(
        string claimValue)
    {
        var identity =
            CreateIdentity(
                new[]
                {
                    new Claim(
                        JwtClaimNames.EmployeeId,
                        claimValue)
                });

        var currentUser =
            CreateCurrentUser(
                identity);

        Assert.True(
            currentUser.IsAuthenticated);

        Assert.Null(
            currentUser.EmployeeId);
    }

    [Theory]
    [InlineData("manager")]
    [InlineData("3")]
    [InlineData("NotARole")]
    public void Role_WithInvalidRepresentation_ReturnsNull(
        string claimValue)
    {
        var identity =
            CreateIdentity(
                new[]
                {
                    new Claim(
                        JwtClaimNames.Role,
                        claimValue)
                });

        var currentUser =
            CreateCurrentUser(
                identity);

        Assert.True(
            currentUser.IsAuthenticated);

        Assert.Null(
            currentUser.Role);
    }

    [Fact]
    public void Email_WithWhitespaceValue_ReturnsNull()
    {
        var identity =
            CreateIdentity(
                new[]
                {
                    new Claim(
                        JwtRegisteredClaimNames.Email,
                        "   ")
                });

        var currentUser =
            CreateCurrentUser(
                identity);

        Assert.True(
            currentUser.IsAuthenticated);

        Assert.Null(
            currentUser.Email);
    }

    private static HttpContextCurrentUser CreateCurrentUser(
        params ClaimsIdentity[] identities)
    {
        var httpContext =
            new DefaultHttpContext
            {
                User =
                    new ClaimsPrincipal(
                        identities)
            };

        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext =
                    httpContext
            };

        return new HttpContextCurrentUser(
            httpContextAccessor);
    }

    private static ClaimsIdentity CreateIdentity(
        IEnumerable<Claim> claims,
        bool isAuthenticated = true)
    {
        return new ClaimsIdentity(
            claims,
            isAuthenticated
                ? "TestAuthentication"
                : null);
    }
}
