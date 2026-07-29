using LeaveManagementSystem.Application.Employees.Services;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.Employees.Services;

public sealed class EmployeeService(
    AppDbContext dbContext)
    : IEmployeeService
{
    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .FirstOrDefaultAsync(
                employee => employee.Id == id,
                cancellationToken);

        if (employee is null)
        {
            return false;
        }

        employee.IsActive = false;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}
