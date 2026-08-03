using System.IdentityModel.Tokens.Jwt;
using LeaveManagementSystem.Application.Authentication.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.IntegrationTests.TestSupport;

[ApiController]
[Route("api/test-authentication")]
public sealed class TestAuthenticationController
    : ControllerBase
{
    [Authorize]
    [HttpGet("claims")]
    [ProducesResponseType(
        typeof(AuthenticationClaimsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public ActionResult<AuthenticationClaimsResponse> GetClaims()
    {
        return Ok(
            new AuthenticationClaimsResponse(
                User.FindFirst(
                    JwtRegisteredClaimNames.Sub)?.Value,
                User.FindFirst(
                    JwtClaimNames.EmployeeId)?.Value,
                User.FindFirst(
                    JwtRegisteredClaimNames.Email)?.Value,
                User.FindFirst(
                    JwtClaimNames.Role)?.Value,
                User.Identity?.IsAuthenticated ?? false));
    }
}

public sealed record AuthenticationClaimsResponse(
    string? UserAccountId,
    string? EmployeeId,
    string? Email,
    string? Role,
    bool IsAuthenticated);
