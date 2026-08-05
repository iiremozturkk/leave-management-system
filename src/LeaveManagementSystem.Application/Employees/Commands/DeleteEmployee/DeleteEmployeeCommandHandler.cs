using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Domain.Enums;
using MediatR;

namespace LeaveManagementSystem.Application.Employees.Commands.DeleteEmployee;

public sealed class DeleteEmployeeCommandHandler(
    ICurrentUserAccessService currentUserAccessService,
    IEmployeeWriteRepository employeeWriteRepository)
    : IRequestHandler<DeleteEmployeeCommand, bool>
{
    public async Task<bool> Handle(
        DeleteEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

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
            return false;
        }

        if (!employee.IsActive)
        {
            return true;
        }

        var hasActiveDirectReports =
            await employeeWriteRepository.HasActiveDirectReportsAsync(
                employee.Id,
                cancellationToken);

        if (hasActiveDirectReports)
        {
            throw new BusinessRuleException(
                "An employee with active direct reports cannot be deactivated.");
        }

        employee.IsActive = false;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await employeeWriteRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}
