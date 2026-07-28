using LeaveManagementSystem.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.IntegrationTests.TestSupport;

public sealed record TestBusinessRuleCommand(
    string Message) : IRequest<string>;

public sealed class TestBusinessRuleCommandHandler
    : IRequestHandler<TestBusinessRuleCommand, string>
{
    public Task<string> Handle(
        TestBusinessRuleCommand request,
        CancellationToken cancellationToken)
    {
        throw new BusinessRuleException(
            request.Message);
    }
}

public sealed record TestBusinessRuleRequest(
    string Message);

[ApiController]
[Route("__test/business-rule")]
public sealed class TestBusinessRuleController : ControllerBase
{
    private readonly ISender _sender;

    public TestBusinessRuleController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<string>> Execute(
        TestBusinessRuleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new TestBusinessRuleCommand(
                request.Message),
            cancellationToken);

        return Ok(result);
    }
}
