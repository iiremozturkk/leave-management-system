using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.WebAPI.Controllers;

[ApiController]
[Route("api/leave-requests")]
public sealed class LeaveRequestsController(ILeaveRequestService leaveRequestService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var leaveRequests = await leaveRequestService.GetAllAsync(cancellationToken);

        return Ok(leaveRequests);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveRequestDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var leaveRequest = await leaveRequestService.GetByIdAsync(id, cancellationToken);

        if (leaveRequest is null)
        {
            return NotFound();
        }

        return Ok(leaveRequest);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LeaveRequestDto>> Create(
        [FromBody] CreateLeaveRequestRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        try
        {
            var leaveRequest = await leaveRequestService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = leaveRequest.Id },
                leaveRequest);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequestProblem(exception.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveRequestDto>> Update(
        Guid id,
        [FromBody] UpdateLeaveRequestRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        try
        {
            var leaveRequest = await leaveRequestService.UpdateAsync(id, request, cancellationToken);

            if (leaveRequest is null)
            {
                return NotFound();
            }

            return Ok(leaveRequest);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequestProblem(exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await leaveRequestService.DeleteAsync(id, cancellationToken);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequestProblem(exception.Message);
        }
    }

    private BadRequestObjectResult BadRequestProblem(string detail)
    {
        return BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid leave request.",
            Detail = detail
        });
    }
}
