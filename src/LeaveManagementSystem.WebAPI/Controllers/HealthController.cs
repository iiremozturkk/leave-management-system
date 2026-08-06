using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LeaveManagementSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController(
    HealthCheckService healthCheckService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var report =
            await healthCheckService.CheckHealthAsync(
                cancellationToken);

        var response = new
        {
            status = report.Status.ToString(),
            service = "LeaveManagementSystem.WebAPI"
        };

        return report.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                response);
    }
}
