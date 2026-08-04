using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeaveManagementSystem.Application.Authentication.Constants;
using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.WebAPI.Authentication.Jwt;

public sealed class RequiredJwtClaimsValidator
{
    public bool HasValidRequiredClaims(
        ClaimsPrincipal? principal)
    {
        var authenticatedIdentity =
            GetSingleAuthenticatedIdentity(
                principal);

        if (authenticatedIdentity is null)
        {
            return false;
        }

        return
            HasValidGuidClaim(
                authenticatedIdentity,
                JwtRegisteredClaimNames.Sub)
            && HasValidGuidClaim(
                authenticatedIdentity,
                JwtRegisteredClaimNames.Jti)
            && HasValidStringClaim(
                authenticatedIdentity,
                JwtRegisteredClaimNames.Email)
            && HasValidGuidClaim(
                authenticatedIdentity,
                JwtClaimNames.EmployeeId)
            && HasValidRoleClaim(
                authenticatedIdentity);
    }

    private static ClaimsIdentity? GetSingleAuthenticatedIdentity(
        ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        var authenticatedIdentities =
            principal
                .Identities
                .Where(identity =>
                    identity.IsAuthenticated)
                .ToArray();

        return authenticatedIdentities.Length == 1
            ? authenticatedIdentities[0]
            : null;
    }

    private static bool HasValidGuidClaim(
        ClaimsIdentity identity,
        string claimType)
    {
        var claimValue =
            GetSingleNonEmptyClaimValue(
                identity,
                claimType);

        return Guid.TryParse(
                   claimValue,
                   out var parsedValue)
               && parsedValue != Guid.Empty;
    }

    private static bool HasValidStringClaim(
        ClaimsIdentity identity,
        string claimType)
    {
        return GetSingleNonEmptyClaimValue(
                   identity,
                   claimType)
               is not null;
    }

    private static bool HasValidRoleClaim(
        ClaimsIdentity identity)
    {
        var claimValue =
            GetSingleNonEmptyClaimValue(
                identity,
                JwtClaimNames.Role);

        if (!Enum.TryParse<EmployeeRole>(
                claimValue,
                ignoreCase: false,
                out var parsedRole))
        {
            return false;
        }

        var definedRoleName =
            Enum.GetName(
                parsedRole);

        return definedRoleName == claimValue;
    }

    private static string? GetSingleNonEmptyClaimValue(
        ClaimsIdentity identity,
        string claimType)
    {
        var matchingClaims =
            identity
                .FindAll(claimType)
                .ToArray();

        if (matchingClaims.Length != 1)
        {
            return null;
        }

        var claimValue =
            matchingClaims[0].Value;

        return string.IsNullOrWhiteSpace(
            claimValue)
            ? null
            : claimValue;
    }
}
