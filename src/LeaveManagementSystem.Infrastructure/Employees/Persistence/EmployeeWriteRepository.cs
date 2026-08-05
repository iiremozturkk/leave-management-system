using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.Employees.Persistence;

public sealed class EmployeeWriteRepository(
    AppDbContext dbContext)
    : IEmployeeWriteRepository
{
    public Task<Employee?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Employees.FirstOrDefaultAsync(
            employee => employee.Id == id,
            cancellationToken);
    }

    public Task<bool> DepartmentExistsAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Departments.AnyAsync(
            department => department.Id == departmentId,
            cancellationToken);
    }

    public Task<bool> ActiveManagerExistsAsync(
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Employees.AnyAsync(
            employee =>
                employee.Id == managerId
                && employee.IsActive
                && employee.Role == EmployeeRole.Manager,
            cancellationToken);
    }

    public Task<Guid?> GetManagerIdAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => employee.ManagerId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasActiveDirectReportsAsync(
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Employees.AnyAsync(
            employee =>
                employee.ManagerId == managerId
                && employee.IsActive,
            cancellationToken);
    }

    public async Task<bool> IsSoleActiveHrAdministratorAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var activeHrEmployeeIds =
            await dbContext.UserAccounts
                .Where(
                    userAccount =>
                        userAccount.IsActive
                        && userAccount.Employee.IsActive
                        && userAccount.Employee.Role ==
                            EmployeeRole.HR)
                .Select(
                    userAccount =>
                        userAccount.EmployeeId)
                .Take(2)
                .ToListAsync(
                    cancellationToken);

        return activeHrEmployeeIds.Count == 1
            && activeHrEmployeeIds[0] == employeeId;
    }

    public Task<bool> EmailExistsAsync(
        string email,
        Guid? excludedEmployeeId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Employees.AnyAsync(
            employee =>
                employee.Email == email
                && (!excludedEmployeeId.HasValue
                    || employee.Id != excludedEmployeeId.Value),
            cancellationToken);
    }

    public void Add(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        dbContext.Employees.Add(employee);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
