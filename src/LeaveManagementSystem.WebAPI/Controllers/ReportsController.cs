using LeaveManagementSystem.Application.Reports.Dtos;
using LeaveManagementSystem.Application.Reports.Queries.GetDepartmentLeaveStatistics;
using LeaveManagementSystem.WebAPI.Authorization.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.WebAPI.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = AuthorizationPolicyNames.HrOnly)]
public sealed class ReportsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet("department-leave-statistics")]
    [ProducesResponseType(
        typeof(IReadOnlyList<DepartmentLeaveStatisticsDto>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<IReadOnlyList<DepartmentLeaveStatisticsDto>>>
        GetDepartmentLeaveStatistics(
            CancellationToken cancellationToken)
    {
        var statistics =
            await sender.Send(
                new GetDepartmentLeaveStatisticsQuery(),
                cancellationToken);

        return Ok(
            statistics);
    }
}
