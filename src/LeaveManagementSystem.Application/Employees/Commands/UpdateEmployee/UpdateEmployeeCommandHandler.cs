using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.Employees.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandler(
    IEmployeeWriteRepository employeeWriteRepository,
    IEmployeeReadRepository employeeReadRepository)
    : IRequestHandler<UpdateEmployeeCommand, EmployeeDto?>
{
    public async Task<EmployeeDto?> Handle(
        UpdateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var employee =
            await employeeWriteRepository.GetForUpdateAsync(
                request.Id,
                cancellationToken);

        if (employee is null)
        {
            return null;
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var departmentExists =
            await employeeWriteRepository.DepartmentExistsAsync(
                request.DepartmentId,
                cancellationToken);

        if (!departmentExists)
        {
            throw new BusinessRuleException(
                "Department does not exist.");
        }

        if (request.ManagerId.HasValue
            && request.ManagerId.Value == request.Id)
        {
            throw new BusinessRuleException(
                "An employee cannot be their own manager.");
        }

        if (request.ManagerId.HasValue)
        {
            var managerExists =
                await employeeWriteRepository.ActiveEmployeeExistsAsync(
                    request.ManagerId.Value,
                    cancellationToken);

            if (!managerExists)
            {
                throw new BusinessRuleException(
                    "Manager does not exist or is not active.");
            }
        }

        var emailExists =
            await employeeWriteRepository.EmailExistsAsync(
                email,
                excludedEmployeeId: request.Id,
                cancellationToken);

        if (emailExists)
        {
            throw new BusinessRuleException(
                "Email is already used by another employee.");
        }

        employee.FirstName = firstName;
        employee.LastName = lastName;
        employee.Email = email;
        employee.DepartmentId = request.DepartmentId;
        employee.ManagerId = request.ManagerId;
        employee.Role = request.Role;
        employee.IsActive = request.IsActive;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await employeeWriteRepository.SaveChangesAsync(
            cancellationToken);

        return await employeeReadRepository.GetByIdAsync(
            employee.Id,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Employee was updated but could not be loaded.");
    }
}
