using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.Employees.Dtos;
using LeaveManagementSystem.Domain.Entities;
using MediatR;

namespace LeaveManagementSystem.Application.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler(
    IEmployeeWriteRepository employeeWriteRepository,
    IEmployeeReadRepository employeeReadRepository)
    : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
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
                excludedEmployeeId: null,
                cancellationToken);

        if (emailExists)
        {
            throw new BusinessRuleException(
                "Email is already used by another employee.");
        }

        var employee = new Employee
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            DepartmentId = request.DepartmentId,
            ManagerId = request.ManagerId,
            Role = request.Role,
            IsActive = true
        };

        employeeWriteRepository.Add(employee);

        await employeeWriteRepository.SaveChangesAsync(
            cancellationToken);

        return await employeeReadRepository.GetByIdAsync(
            employee.Id,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Employee was created but could not be loaded.");
    }
}
