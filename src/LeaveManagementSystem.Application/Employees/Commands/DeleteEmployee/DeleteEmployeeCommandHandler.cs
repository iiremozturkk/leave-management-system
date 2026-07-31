using LeaveManagementSystem.Application.Employees.Abstractions;
using MediatR;

namespace LeaveManagementSystem.Application.Employees.Commands.DeleteEmployee;

public sealed class DeleteEmployeeCommandHandler(
    IEmployeeWriteRepository employeeWriteRepository)
    : IRequestHandler<DeleteEmployeeCommand, bool>
{
    public async Task<bool> Handle(
        DeleteEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var employee =
            await employeeWriteRepository.GetForUpdateAsync(
                request.Id,
                cancellationToken);

        if (employee is null)
        {
            return false;
        }

        employee.IsActive = false;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await employeeWriteRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}
