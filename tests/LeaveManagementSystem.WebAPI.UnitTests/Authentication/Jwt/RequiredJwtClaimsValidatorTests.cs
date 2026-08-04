using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeaveManagementSystem.Application.Authentication.Constants;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.WebAPI.Authentication.Jwt;
using Xunit;

namespace LeaveManagementSystem.WebAPI.UnitTests.Authentication.Jwt;

public sealed class RequiredJwtClaimsValidatorTests
{
    private static readonly string[] RequiredClaimTypeValues =
    {
        JwtRegisteredClaimNames.Sub,
        JwtRegisteredClaimNames.Jti,
        JwtRegisteredClaimNames.Email,
        JwtClaimNames.EmployeeId,
        JwtClaimNames.Role
    };

    private readonly RequiredJwtClaimsValidator _validator =
        new();

    public static IEnumerable<object[]> RequiredClaimTypes
    {
        get
        {
            foreach (var claimType in RequiredClaimTypeValues)
            {
                yield return
                    new object[]
                    {
                        claimType
                    };
            }
        }
    }

    public static IEnumerable<object[]> InvalidRequiredClaimValues
    {
        get
        {
            foreach (var claimType in RequiredClaimTypeValues)
            {
                yield return
                    new object[]
                    {
                        claimType,
                        string.Empty
                    };

                yield return
                    new object[]
                    {
                        claimType,
                        "   "
                    };
            }
        }
    }

    public static IEnumerable<object[]> InvalidGuidClaims
    {
        get
        {
            var claimTypes =
                new[]
                {
                    JwtRegisteredClaimNames.Sub,
                    JwtRegisteredClaimNames.Jti,
                    JwtClaimNames.EmployeeId
                };

            var invalidValues =
                new[]
                {
                    "not-a-guid",
                    Guid.Empty.ToString("D")
                };

            foreach (var claimType in claimTypes)
            {
                foreach (var invalidValue in invalidValues)
                {
                    yield return
                        new object[]
                        {
                            claimType,
                            invalidValue
                        };
                }
            }
        }
    }

    [Fact]
    public void HasValidRequiredClaims_WithValidClaims_ReturnsTrue()
    {
        var principal =
            CreateAuthenticatedPrincipal(
                CreateRequiredClaims());

        var result =
            _validator.HasValidRequiredClaims(
                principal);

        Assert.True(
            result);
    }

    [Fact]
    public void HasValidRequiredClaims_WithNullPrincipal_ReturnsFalse()
    {
        var result =
            _validator.HasValidRequiredClaims(
                null);

        Assert.False(
            result);
    }

    [Fact]
    public void HasValidRequiredClaims_WithoutAuthenticatedIdentity_ReturnsFalse()
    {
        var unauthenticatedIdentity =
            CreateIdentity(
                CreateRequiredClaims(),
                isAuthenticated: false);

        var principal =
            new ClaimsPrincipal(
                unauthenticatedIdentity);

        var result =
            _validator.HasValidRequiredClaims(
                principal);

        Assert.False(
            result);
    }

    [Fact]
    public void HasValidRequiredClaims_WithMultipleAuthenticatedIdentities_ReturnsFalse()
    {
        var firstIdentity =
            CreateIdentity(
                CreateRequiredClaims());

        var secondIdentity =
            CreateIdentity(
                CreateRequiredClaims());

        var principal =
            new ClaimsPrincipal(
                new[]
                {
                    firstIdentity,
                    secondIdentity
                });

        var result =
            _validator.HasValidRequiredClaims(
                principal);

        Assert.False(
            result);
    }

    [Fact]
    public void HasValidRequiredClaims_DoesNotReadClaimsFromUnauthenticatedIdentity()
    {
        var authenticatedIdentity =
            CreateIdentity(
                Array.Empty<Claim>());

        var unauthenticatedIdentity =
            CreateIdentity(
                CreateRequiredClaims(),
                isAuthenticated: false);

        var principal =
            new ClaimsPrincipal(
                new[]
                {
                    authenticatedIdentity,
                    unauthenticatedIdentity
                });

        var result =
            _validator.HasValidRequiredClaims(
                principal);

        Assert.False(
            result);
    }

    [Fact]
    public void HasValidRequiredClaims_WithValidAuthenticatedAndUnauthenticatedIdentity_ReturnsTrue()
    {
        var authenticatedIdentity =
            CreateIdentity(
                CreateRequiredClaims());

        var unauthenticatedIdentity =
            CreateIdentity(
                new[]
                {
                    new Claim(
                        JwtRegisteredClaimNames.Sub,
                        "invalid-sub"),

                    new Claim(
                        JwtClaimNames.Role,
                        "invalid-role")
                },
                isAuthenticated: false);

        var principal =
            new ClaimsPrincipal(
                new[]
                {
                    authenticatedIdentity,
                    unauthenticatedIdentity
                });

        var result =
            _validator.HasValidRequiredClaims(
                principal);

        Assert.True(
            result);
    }

    [Theory]
    [MemberData(nameof(RequiredClaimTypes))]
    public void HasValidRequiredClaims_WithMissingRequiredClaim_ReturnsFalse(
        string claimType)
    {
        var claims =
            CreateRequiredClaims();

        claims.RemoveAll(
            claim =>
                claim.Type == claimType);

        var principal =
            CreateAuthenticatedPrincipal(
                claims);

        var result =
            _validator.HasValidRequiredClaims(
                principal);

        Assert.False(
            result);
    }

    [Theory]
    [MemberData(nameof(RequiredClaimTypes))]
    public void HasValidRequiredClaims_WithDuplicateRequiredClaim_ReturnsFalse(
        string claimType)
    {
        var claims =
            CreateRequiredClaims();

        var existingClaim =
            Assert.Single(
                claims,
                claim =>
                    claim.Type == claimType);

        claims.Add(
            new Claim(
                existingClaim.Type,
                existingClaim.Value));

        var principal =
            CreateAuthenticatedPrincipal(
                claims);

        var result =
            _validator.HasValidRequiredClaims(
                principal);

        Assert.False(
            result);
    }

    [Theory]
    [MemberData(nameof(InvalidRequiredClaimValues))]
    public void HasValidRequiredClaims_WithEmptyOrWhitespaceRequiredClaim_ReturnsFalse(
        string claimType,
        string claimValue)
    {
        var claims =
            CreateRequiredClaims();

        ReplaceClaim(
            claims,
            claimType,
            claimValue);

        var principal =
            CreateAuthenticatedPrincipal(
                claims);

        var result =
            _validator.HasValidRequiredClaims(
                principal);

        Assert.False(
            result);
    }

    [Theory]
    [MemberData(nameof(InvalidGuidClaims))]
    public void HasValidRequiredClaims_WithInvalidGuidClaim_ReturnsFalse(
        string claimType,
        string claimValue)
    {
        var claims =
            CreateRequiredClaims();

        ReplaceClaim(
            claims,
            claimType,
            claimValue);

        var principal =
            CreateAuthenticatedPrincipal(
                claims);

        var result =
            _validator.HasValidRequiredClaims(
                principal);

        Assert.False(
            result);
    }

    [Theory]
    [InlineData("manager")]
    [InlineData("3")]
    [InlineData("NotARole")]
    public void HasValidRequiredClaims_WithInvalidRoleRepresentation_ReturnsFalse(
        string claimValue)
    {
        var claims =
            CreateRequiredClaims();

        ReplaceClaim(
            claims,
            JwtClaimNames.Role,
            claimValue);

        var principal =
            CreateAuthenticatedPrincipal(
                claims);

        var result =
            _validator.HasValidRequiredClaims(
                principal);

        Assert.False(
            result);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(
        IEnumerable<Claim> claims)
    {
        return new ClaimsPrincipal(
            CreateIdentity(
                claims));
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

    private static List<Claim> CreateRequiredClaims()
    {
        return
            new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    Guid.NewGuid().ToString("D")),

                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString("D")),

                new(
                    JwtRegisteredClaimNames.Email,
                    "jwt.claims@example.com"),

                new(
                    JwtClaimNames.EmployeeId,
                    Guid.NewGuid().ToString("D")),

                new(
                    JwtClaimNames.Role,
                    EmployeeRole.Manager.ToString())
            };
    }

    private static void ReplaceClaim(
        List<Claim> claims,
        string claimType,
        string replacementValue)
    {
        claims.RemoveAll(
            claim =>
                claim.Type == claimType);

        claims.Add(
            new Claim(
                claimType,
                replacementValue));
    }
}
