using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Commands.ApproveLeaveRequest;
using LeaveManagementSystem.Application.LeaveRequests.Commands.CreateLeaveRequest;
using LeaveManagementSystem.Application.LeaveRequests.Commands.DeleteLeaveRequest;
using LeaveManagementSystem.Application.LeaveRequests.Commands.RejectLeaveRequest;
using LeaveManagementSystem.Application.LeaveRequests.Commands.UpdateLeaveRequest;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveBalance;
using LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequestById;
using LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveRequests;
using LeaveManagementSystem.WebAPI.Authorization.Policies;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.WebAPI.Controllers;

[ApiController]
[Route("api/leave-requests")]
public sealed class LeaveRequestsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [Authorize(
        Policy = AuthorizationPolicyNames.AuthenticatedEmployee)]
    [ProducesResponseType(
        typeof(IReadOnlyList<LeaveRequestDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var leaveRequests = await sender.Send(
            new GetLeaveRequestsQuery(),
            cancellationToken);

        return Ok(leaveRequests);
    }

    [HttpGet("{id:guid}")]
    [Authorize(
        Policy = AuthorizationPolicyNames.AuthenticatedEmployee)]
    [ProducesResponseType(
        typeof(LeaveRequestDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveRequestDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var leaveRequest = await sender.Send(
            new GetLeaveRequestByIdQuery(id),
            cancellationToken);

        if (leaveRequest is null)
        {
            return NotFound();
        }

        return Ok(leaveRequest);
    }

    [HttpGet("balance")]
    [Authorize(
        Policy = AuthorizationPolicyNames.AuthenticatedEmployee)]
    [ProducesResponseType(
        typeof(LeaveBalanceDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveBalanceDto>> GetBalance(
        [FromQuery] Guid leaveTypeId,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        try
        {
            var balance = await sender.Send(
                new GetLeaveBalanceQuery(
                    leaveTypeId,
                    year),
                cancellationToken);

            if (balance is null)
            {
                return NotFound();
            }

            return Ok(balance);
        }
        catch (InvalidOperationException exception)
            when (exception is not ForbiddenOperationException)
        {
            return BadRequestProblem(
                exception.Message);
        }
    }

    [HttpPost]
    [Authorize(
        Policy = AuthorizationPolicyNames.AuthenticatedEmployee)]
    [ProducesResponseType(
        typeof(LeaveRequestDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LeaveRequestDto>> Create(
        CreateLeaveRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequest = await sender.Send(
                new CreateLeaveRequestCommand(
                    request.LeaveTypeId,
                    request.StartDate,
                    request.EndDate,
                    request.Reason),
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = leaveRequest.Id },
                leaveRequest);
        }
        catch (InvalidOperationException exception)
            when (exception is not ForbiddenOperationException)
        {
            return BadRequestProblem(
                exception.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(
        Policy = AuthorizationPolicyNames.AuthenticatedEmployee)]
    [ProducesResponseType(
        typeof(LeaveRequestDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveRequestDto>> Update(
        Guid id,
        UpdateLeaveRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequest = await sender.Send(
                new UpdateLeaveRequestCommand(
                    id,
                    request.LeaveTypeId,
                    request.StartDate,
                    request.EndDate,
                    request.Reason),
                cancellationToken);

            if (leaveRequest is null)
            {
                return NotFound();
            }

            return Ok(leaveRequest);
        }
        catch (InvalidOperationException exception)
            when (exception is not ForbiddenOperationException)
        {
            return BadRequestProblem(
                exception.Message);
        }
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(
        Policy = AuthorizationPolicyNames.ManagerOnly)]
    [ProducesResponseType(
        typeof(LeaveRequestDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveRequestDto>> Approve(
        Guid id,
        ReviewLeaveRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequest =
                await sender.Send(
                    new ApproveLeaveRequestCommand(
                        id,
                        request.ManagerComment),
                    cancellationToken);

            if (leaveRequest is null)
            {
                return NotFound();
            }

            return Ok(leaveRequest);
        }
        catch (InvalidOperationException exception)
            when (exception is not ForbiddenOperationException)
        {
            return BadRequestProblem(
                exception.Message);
        }
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(
        Policy = AuthorizationPolicyNames.ManagerOnly)]
    [ProducesResponseType(
        typeof(LeaveRequestDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveRequestDto>> Reject(
        Guid id,
        ReviewLeaveRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var leaveRequest =
                await sender.Send(
                    new RejectLeaveRequestCommand(
                        id,
                        request.ManagerComment),
                    cancellationToken);

            if (leaveRequest is null)
            {
                return NotFound();
            }

            return Ok(leaveRequest);
        }
        catch (InvalidOperationException exception)
            when (exception is not ForbiddenOperationException)
        {
            return BadRequestProblem(
                exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(
        Policy = AuthorizationPolicyNames.AuthenticatedEmployee)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted =
                await sender.Send(
                    new DeleteLeaveRequestCommand(
                        id),
                    cancellationToken);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException exception)
            when (exception is not ForbiddenOperationException)
        {
            return BadRequestProblem(
                exception.Message);
        }
    }

    private BadRequestObjectResult BadRequestProblem(
        string detail)
    {
        return BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid leave request.",
            Detail = detail
        });
    }
}
