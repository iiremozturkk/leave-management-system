using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Constants;
using LeaveManagementSystem.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace LeaveManagementSystem.WebAPI.Authentication.CurrentUser;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor =
            httpContextAccessor
            ?? throw new ArgumentNullException(
                nameof(httpContextAccessor));
    }

    public bool IsAuthenticated =>
        GetAuthenticatedIdentity() is not null;

    public Guid? UserAccountId =>
        GetGuidClaim(
            JwtRegisteredClaimNames.Sub);

    public Guid? EmployeeId =>
        GetGuidClaim(
            JwtClaimNames.EmployeeId);

    public string? Email =>
        GetSingleClaimValue(
            JwtRegisteredClaimNames.Email);

    public EmployeeRole? Role =>
        GetEmployeeRoleClaim();

    private ClaimsIdentity? GetAuthenticatedIdentity()
    {
        var principal =
            _httpContextAccessor
                .HttpContext?
                .User;

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

    private Guid? GetGuidClaim(
        string claimType)
    {
        var claimValue =
            GetSingleClaimValue(
                claimType);

        if (!Guid.TryParse(
                claimValue,
                out var parsedValue)
            || parsedValue == Guid.Empty)
        {
            return null;
        }

        return parsedValue;
    }

    private EmployeeRole? GetEmployeeRoleClaim()
    {
        var claimValue =
            GetSingleClaimValue(
                JwtClaimNames.Role);

        if (!Enum.TryParse<EmployeeRole>(
                claimValue,
                ignoreCase: false,
                out var parsedRole))
        {
            return null;
        }

        var definedRoleName =
            Enum.GetName(parsedRole);

        return definedRoleName == claimValue
            ? parsedRole
            : null;
    }

    private string? GetSingleClaimValue(
        string claimType)
    {
        var identity =
            GetAuthenticatedIdentity();

        if (identity is null)
        {
            return null;
        }

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
