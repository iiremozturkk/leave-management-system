using LeaveManagementSystem.Application.Employees.Commands.CreateEmployee;
using LeaveManagementSystem.Application.Employees.Dtos;
using LeaveManagementSystem.Application.Employees.Queries.GetEmployeeById;
using LeaveManagementSystem.Application.Employees.Queries.GetEmployees;
using LeaveManagementSystem.Application.Employees.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.WebAPI.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(
    ISender sender,
    IEmployeeService employeeService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<EmployeeDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var employees = await sender.Send(
            new GetEmployeesQuery(),
            cancellationToken);

        return Ok(employees);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(EmployeeDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var employee = await sender.Send(
            new GetEmployeeByIdQuery(id),
            cancellationToken);

        if (employee is null)
        {
            return NotFound();
        }

        return Ok(employee);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(EmployeeDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmployeeDto>> Create(
        [FromBody] CreateEmployeeRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequestProblem(
                "Request body is required.");
        }

        var employee = await sender.Send(
            new CreateEmployeeCommand(
                request.FirstName,
                request.LastName,
                request.Email,
                request.DepartmentId,
                request.ManagerId,
                request.Role),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = employee.Id },
            employee);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(EmployeeDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDto>> Update(
        Guid id,
        [FromBody] UpdateEmployeeRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequestProblem(
                "Request body is required.");
        }

        try
        {
            var employee = await employeeService.UpdateAsync(
                id,
                request,
                cancellationToken);

            if (employee is null)
            {
                return NotFound();
            }

            return Ok(employee);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequestProblem(exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
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
            var deleted = await employeeService.DeleteAsync(
                id,
                cancellationToken);

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

    private BadRequestObjectResult BadRequestProblem(
        string detail)
    {
        return BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid employee request.",
            Detail = detail
        });
    }
}
