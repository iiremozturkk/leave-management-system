using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "LeaveManagementSystem.WebAPI"
        });
    }
}
