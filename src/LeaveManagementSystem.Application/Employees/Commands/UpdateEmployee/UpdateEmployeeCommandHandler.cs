using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.Employees.Dtos;
using LeaveManagementSystem.Domain.Enums;
using MediatR;

namespace LeaveManagementSystem.Application.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandler(
    ICurrentUserAccessService currentUserAccessService,
    IEmployeeWriteRepository employeeWriteRepository,
    IEmployeeReadRepository employeeReadRepository,
    IEmployeeAdministrationTransactionManager
        employeeAdministrationTransactionManager)
    : IRequestHandler<UpdateEmployeeCommand, EmployeeDto?>
{
    public async Task<EmployeeDto?> Handle(
        UpdateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction =
            await employeeAdministrationTransactionManager.BeginAsync(
                cancellationToken);

        var currentUserAccess =
            await currentUserAccessService.GetAsync(
                cancellationToken);

        if (currentUserAccess is null
            || currentUserAccess.Role != EmployeeRole.HR)
        {
            throw new ForbiddenOperationException(
                "Only current active HR employees can administer employees.");
        }

        var employee =
            await employeeWriteRepository.GetForUpdateAsync(
                request.Id,
                cancellationToken);

        if (employee is null)
        {
            return null;
        }

        var firstName =
            request.FirstName.Trim();

        var lastName =
            request.LastName.Trim();

        var email =
            request.Email
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
                await employeeWriteRepository.ActiveManagerExistsAsync(
                    request.ManagerId.Value,
                    cancellationToken);

            if (!managerExists)
            {
                throw new BusinessRuleException(
                    "Manager does not exist, is not active, or does not have the Manager role.");
            }
        }

        var reactivatesEmployee =
            !employee.IsActive
            && request.IsActive;

        if (request.ManagerId.HasValue
            && (request.ManagerId != employee.ManagerId
                || reactivatesEmployee))
        {
            await EnsureManagerHierarchyDoesNotContainCycleAsync(
                employee.Id,
                request.ManagerId.Value,
                employeeWriteRepository,
                cancellationToken);
        }

        var demotesManager =
            employee.Role == EmployeeRole.Manager
            && request.Role != EmployeeRole.Manager;

        var deactivatesManager =
            employee.Role == EmployeeRole.Manager
            && employee.IsActive
            && !request.IsActive;

        if (demotesManager || deactivatesManager)
        {
            var hasActiveDirectReports =
                await employeeWriteRepository
                    .HasActiveDirectReportsAsync(
                        employee.Id,
                        cancellationToken);

            if (hasActiveDirectReports)
            {
                throw new BusinessRuleException(
                    "A manager with active direct reports cannot be deactivated or assigned another role.");
            }
        }

        var removesActiveHrAdministrator =
            employee.Role == EmployeeRole.HR
            && employee.IsActive
            && (request.Role != EmployeeRole.HR
                || !request.IsActive);

        if (removesActiveHrAdministrator)
        {
            var isSoleActiveHrAdministrator =
                await employeeWriteRepository
                    .IsSoleActiveHrAdministratorAsync(
                        employee.Id,
                        cancellationToken);

            if (isSoleActiveHrAdministrator)
            {
                throw new BusinessRuleException(
                    "The last active HR administrator cannot be deactivated or assigned another role.");
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

        var updatedEmployee =
            await employeeReadRepository.GetByIdAsync(
                employee.Id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Employee was updated but could not be loaded.");

        await transaction.CommitAsync(
            cancellationToken);

        return updatedEmployee;
    }

    private static async Task EnsureManagerHierarchyDoesNotContainCycleAsync(
        Guid employeeId,
        Guid proposedManagerId,
        IEmployeeWriteRepository employeeWriteRepository,
        CancellationToken cancellationToken)
    {
        var visitedEmployeeIds = new HashSet<Guid>
        {
            employeeId
        };

        Guid? currentEmployeeId =
            proposedManagerId;

        while (currentEmployeeId.HasValue)
        {
            if (!visitedEmployeeIds.Add(
                    currentEmployeeId.Value))
            {
                throw new BusinessRuleException(
                    "Manager hierarchy cannot contain a cycle.");
            }

            currentEmployeeId =
                await employeeWriteRepository.GetManagerIdAsync(
                    currentEmployeeId.Value,
                    cancellationToken);
        }
    }
}
