using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LeaveManagementSystem.IntegrationTests.TestSupport;

public sealed record TestValidationCommand(
    string? Name) : IRequest<string>;

public sealed class TestValidationCommandValidator
    : AbstractValidator<TestValidationCommand>
{
    public TestValidationCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("Name is required.");
    }
}

public sealed class TestValidationCommandHandler
    : IRequestHandler<TestValidationCommand, string>
{
    public Task<string> Handle(
        TestValidationCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Name ?? string.Empty);
    }
}

public sealed record TestValidationRequest(string? Name);

[ApiController]
[Route("__test/validation")]
public sealed class TestValidationController : ControllerBase
{
    private readonly ISender _sender;

    public TestValidationController(ISender sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<string>> Validate(
        TestValidationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new TestValidationCommand(request.Name),
            cancellationToken);

        return Ok(result);
    }
}
