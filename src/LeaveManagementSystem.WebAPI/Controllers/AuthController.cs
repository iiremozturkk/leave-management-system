using LeaveManagementSystem.Application.Authentication.Commands.Login;
using LeaveManagementSystem.Application.Authentication.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.WebAPI.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ISender sender)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(LoginResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResult>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LoginCommand(
                request.Email,
                request.Password),
            cancellationToken);

        if (result is null)
        {
            return UnauthorizedProblem();
        }

        return Ok(result);
    }

    private UnauthorizedObjectResult UnauthorizedProblem()
    {
        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Authentication failed.",
            Detail = "Invalid email or password."
        });
    }
}
