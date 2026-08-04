using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace LeaveManagementSystem.WebAPI.Authentication.Jwt;

public sealed class RequiredJwtBearerEvents : JwtBearerEvents
{
    private readonly RequiredJwtClaimsValidator _claimsValidator;

    public RequiredJwtBearerEvents(
        RequiredJwtClaimsValidator claimsValidator)
    {
        _claimsValidator =
            claimsValidator
            ?? throw new ArgumentNullException(
                nameof(claimsValidator));
    }

    public override Task TokenValidated(
        TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        if (!_claimsValidator.HasValidRequiredClaims(
                context.Principal))
        {
            context.Fail(
                "The access token is invalid.");
        }

        return Task.CompletedTask;
    }
}
