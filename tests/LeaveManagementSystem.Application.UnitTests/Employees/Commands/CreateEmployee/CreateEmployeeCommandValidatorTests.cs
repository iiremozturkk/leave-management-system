using FluentValidation.Results;
using LeaveManagementSystem.Application.Employees.Commands.CreateEmployee;
using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Application.UnitTests.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidatorTests
{
    private readonly CreateEmployeeCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldSucceed()
    {
        var command = CreateValidCommand();

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenValuesAreAtMaximumLength_ShouldSucceed()
    {
        var command = CreateValidCommand() with
        {
            FirstName = new string('A', 100),
            LastName = new string('B', 100),
            Email = $"{new string('a', 243)}@example.com"
        };

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WhenFirstNameIsEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            FirstName = string.Empty
        };

        var result = await _validator.ValidateAsync(command);

        AssertSingleFailure(
            result,
            nameof(CreateEmployeeCommand.FirstName),
            "First name is required.");
    }

    [Fact]
    public async Task Validate_WhenFirstNameExceedsMaximumLength_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            FirstName = new string('A', 101)
        };

        var result = await _validator.ValidateAsync(command);

        AssertSingleFailure(
            result,
            nameof(CreateEmployeeCommand.FirstName),
            "First name must not exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WhenLastNameIsEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            LastName = string.Empty
        };

        var result = await _validator.ValidateAsync(command);

        AssertSingleFailure(
            result,
            nameof(CreateEmployeeCommand.LastName),
            "Last name is required.");
    }

    [Fact]
    public async Task Validate_WhenLastNameExceedsMaximumLength_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            LastName = new string('B', 101)
        };

        var result = await _validator.ValidateAsync(command);

        AssertSingleFailure(
            result,
            nameof(CreateEmployeeCommand.LastName),
            "Last name must not exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WhenEmailIsEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Email = string.Empty
        };

        var result = await _validator.ValidateAsync(command);

        AssertSingleFailure(
            result,
            nameof(CreateEmployeeCommand.Email),
            "Email is required.");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("irem@")]
    public async Task Validate_WhenEmailFormatIsInvalid_ShouldFail(
        string invalidEmail)
    {
        var command = CreateValidCommand() with
        {
            Email = invalidEmail
        };

        var result = await _validator.ValidateAsync(command);

        AssertSingleFailure(
            result,
            nameof(CreateEmployeeCommand.Email),
            "Email must be a valid email address.");
    }

    [Fact]
    public async Task Validate_WhenEmailExceedsMaximumLength_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Email = $"{new string('a', 244)}@example.com"
        };

        var result = await _validator.ValidateAsync(command);

        AssertSingleFailure(
            result,
            nameof(CreateEmployeeCommand.Email),
            "Email must not exceed 255 characters.");
    }

    [Fact]
    public async Task Validate_WhenDepartmentIdIsEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            DepartmentId = Guid.Empty
        };

        var result = await _validator.ValidateAsync(command);

        AssertSingleFailure(
            result,
            nameof(CreateEmployeeCommand.DepartmentId),
            "Department id is required.");
    }

    [Fact]
    public async Task Validate_WhenManagerIdIsEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            ManagerId = Guid.Empty
        };

        var result = await _validator.ValidateAsync(command);

        AssertSingleFailure(
            result,
            nameof(CreateEmployeeCommand.ManagerId),
            "Manager id cannot be empty.");
    }

    [Fact]
    public async Task Validate_WhenManagerIdIsNonEmpty_ShouldSucceed()
    {
        var command = CreateValidCommand() with
        {
            ManagerId = Guid.NewGuid()
        };

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public async Task Validate_WhenRoleIsInvalid_ShouldFail(
        int invalidRoleValue)
    {
        var command = CreateValidCommand() with
        {
            Role = (EmployeeRole)invalidRoleValue
        };

        var result = await _validator.ValidateAsync(command);

        AssertSingleFailure(
            result,
            nameof(CreateEmployeeCommand.Role),
            "Employee role is invalid.");
    }

    [Fact]
    public async Task Validate_WhenRequiredTextFieldsContainOnlyWhitespace_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            FirstName = "   ",
            LastName = "\t",
            Email = "   "
        };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);

        Assert.Contains(
            result.Errors,
            failure =>
                failure.PropertyName
                    == nameof(CreateEmployeeCommand.FirstName)
                && failure.ErrorMessage
                    == "First name is required.");

        Assert.Contains(
            result.Errors,
            failure =>
                failure.PropertyName
                    == nameof(CreateEmployeeCommand.LastName)
                && failure.ErrorMessage
                    == "Last name is required.");

        Assert.Contains(
            result.Errors,
            failure =>
                failure.PropertyName
                    == nameof(CreateEmployeeCommand.Email)
                && failure.ErrorMessage
                    == "Email is required.");
    }

    private static void AssertSingleFailure(
        ValidationResult result,
        string expectedPropertyName,
        string expectedErrorMessage)
    {
        Assert.False(result.IsValid);

        var failure = Assert.Single(result.Errors);

        Assert.Equal(
            expectedPropertyName,
            failure.PropertyName);

        Assert.Equal(
            expectedErrorMessage,
            failure.ErrorMessage);
    }

    private static CreateEmployeeCommand CreateValidCommand()
    {
        return new CreateEmployeeCommand(
            FirstName: "Irem",
            LastName: "Ozturk",
            Email: "irem@example.com",
            DepartmentId: Guid.NewGuid(),
            ManagerId: null,
            Role: EmployeeRole.Employee);
    }
}