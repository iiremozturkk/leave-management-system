using FluentValidation;

namespace LeaveManagementSystem.Application.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidator
    : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(command => command.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters.");

        RuleFor(command => command.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters.");

        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.");

        RuleFor(command => command.DepartmentId)
            .NotEmpty()
            .WithMessage("Department id is required.");

        RuleFor(command => command.ManagerId)
            .Must(managerId =>
                managerId is null
                || managerId.Value != Guid.Empty)
            .WithMessage("Manager id cannot be empty.");

        RuleFor(command => command.Role)
            .IsInEnum()
            .WithMessage("Employee role is invalid.");
    }
}
