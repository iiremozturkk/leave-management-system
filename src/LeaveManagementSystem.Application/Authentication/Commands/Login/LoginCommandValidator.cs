using FluentValidation;

namespace LeaveManagementSystem.Application.Authentication.Commands.Login;

public sealed class LoginCommandValidator
    : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.");

        RuleFor(command => command.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MaximumLength(256)
            .WithMessage("Password must not exceed 256 characters.");
    }
}
