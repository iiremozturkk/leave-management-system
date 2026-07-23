using FluentValidation;
using MediatR;

namespace LeaveManagementSystem.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validators = _validators.ToArray();

        if (validators.Length == 0)
        {
            return await next(cancellationToken);
        }

        var validationContext = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(validator =>
                validator.ValidateAsync(validationContext, cancellationToken)));

        var validationFailures = validationResults
            .SelectMany(result => result.Errors)
            .ToArray();

        if (validationFailures.Length > 0)
        {
            throw new ValidationException(validationFailures);
        }

        return await next(cancellationToken);
    }
}
