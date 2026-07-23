using FluentValidation;
using LeaveManagementSystem.Application.Common.Behaviors;
using MediatR;

namespace LeaveManagementSystem.Application.UnitTests;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidatorExists_ShouldExecuteNextHandler()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestRequest, string>(
            Array.Empty<IValidator<TestRequest>>());

        var handlerWasCalled = false;

        RequestHandlerDelegate<string> next = (CancellationToken _) =>
        {
            handlerWasCalled = true;
            return Task.FromResult("Handler executed.");
        };

        var request = new TestRequest(
            "Valid value",
            "Valid description");

        // Act
        var result = await behavior.Handle(
            request,
            next,
            CancellationToken.None);

        // Assert
        Assert.True(handlerWasCalled);
        Assert.Equal("Handler executed.", result);
    }

    [Fact]
    public async Task Handle_WhenValidationSucceeds_ShouldExecuteNextHandler()
    {
        // Arrange
        var validators = new IValidator<TestRequest>[]
        {
            new TestRequestValidator()
        };

        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var handlerWasCalled = false;

        RequestHandlerDelegate<string> next = (CancellationToken _) =>
        {
            handlerWasCalled = true;
            return Task.FromResult("Handler executed.");
        };

        var request = new TestRequest(
            "Valid value",
            "Valid description");

        // Act
        var result = await behavior.Handle(
            request,
            next,
            CancellationToken.None);

        // Assert
        Assert.True(handlerWasCalled);
        Assert.Equal("Handler executed.", result);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowAndNotExecuteNextHandler()
    {
        // Arrange
        var validators = new IValidator<TestRequest>[]
        {
            new TestRequestValidator()
        };

        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var handlerWasCalled = false;

        RequestHandlerDelegate<string> next = (CancellationToken _) =>
        {
            handlerWasCalled = true;
            return Task.FromResult("Handler executed.");
        };

        var request = new TestRequest(
            string.Empty,
            "Valid description");

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(
                request,
                next,
                CancellationToken.None));

        // Assert
        Assert.False(handlerWasCalled);

        var validationFailure = Assert.Single(exception.Errors);

        Assert.Equal(
            nameof(TestRequest.Value),
            validationFailure.PropertyName);

        Assert.Equal(
            "Value is required.",
            validationFailure.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WhenMultipleValidationRulesFail_ShouldCollectAllFailures()
    {
        // Arrange
        var validators = new IValidator<TestRequest>[]
        {
            new TestRequestValidator()
        };

        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var handlerWasCalled = false;

        RequestHandlerDelegate<string> next = (CancellationToken _) =>
        {
            handlerWasCalled = true;
            return Task.FromResult("Handler executed.");
        };

        var request = new TestRequest(
            string.Empty,
            string.Empty);

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(
                request,
                next,
                CancellationToken.None));

        // Assert
        Assert.False(handlerWasCalled);

        var validationFailures = exception.Errors.ToArray();

        Assert.Equal(2, validationFailures.Length);

        Assert.Contains(
            validationFailures,
            failure =>
                failure.PropertyName == nameof(TestRequest.Value) &&
                failure.ErrorMessage == "Value is required.");

        Assert.Contains(
            validationFailures,
            failure =>
                failure.PropertyName == nameof(TestRequest.Description) &&
                failure.ErrorMessage == "Description is required.");
    }

    private sealed record TestRequest(
        string Value,
        string Description) : IRequest<string>;

    private sealed class TestRequestValidator
        : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(request => request.Value)
                .NotEmpty()
                .WithMessage("Value is required.");

            RuleFor(request => request.Description)
                .NotEmpty()
                .WithMessage("Description is required.");
        }
    }
}
