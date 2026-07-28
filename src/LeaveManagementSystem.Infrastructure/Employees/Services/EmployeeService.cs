using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.Employees.Dtos;
using LeaveManagementSystem.Application.Employees.Services;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.Employees.Services;

public sealed class EmployeeService(
    AppDbContext dbContext,
    IEmployeeReadRepository employeeReadRepository)
    : IEmployeeService
{
    public async Task<EmployeeDto?> UpdateAsync(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .FirstOrDefaultAsync(
                employee => employee.Id == id,
                cancellationToken);

        if (employee is null)
        {
            return null;
        }

        var firstName = NormalizeRequiredText(
            request.FirstName,
            "First name");

        var lastName = NormalizeRequiredText(
            request.LastName,
            "Last name");

        var email = NormalizeEmail(request.Email);

        await EnsureDepartmentExistsAsync(
            request.DepartmentId,
            cancellationToken);

        await EnsureManagerCanBeAssignedAsync(
            request.ManagerId,
            id,
            cancellationToken);

        await EnsureEmailIsUniqueAsync(
            email,
            id,
            cancellationToken);

        employee.FirstName = firstName;
        employee.LastName = lastName;
        employee.Email = email;
        employee.DepartmentId = request.DepartmentId;
        employee.ManagerId = request.ManagerId;
        employee.Role = request.Role;
        employee.IsActive = request.IsActive;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await employeeReadRepository.GetByIdAsync(
            employee.Id,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Employee was updated but could not be loaded.");
    }

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

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task EnsureDepartmentExistsAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        if (departmentId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Department id cannot be empty.");
        }

        var exists = await dbContext.Departments
            .AnyAsync(
                department => department.Id == departmentId,
                cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Department does not exist.");
        }
    }

    private async Task EnsureManagerCanBeAssignedAsync(
        Guid? managerId,
        Guid? employeeId,
        CancellationToken cancellationToken)
    {
        if (managerId is null)
        {
            return;
        }

        if (managerId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Manager id cannot be empty.");
        }

        if (employeeId is not null
            && managerId.Value == employeeId.Value)
        {
            throw new InvalidOperationException(
                "An employee cannot be their own manager.");
        }

        var exists = await dbContext.Employees
            .AnyAsync(
                employee =>
                    employee.Id == managerId.Value
                    && employee.IsActive,
                cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Manager does not exist or is not active.");
        }
    }

    private async Task EnsureEmailIsUniqueAsync(
        string email,
        Guid? currentEmployeeId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Employees
            .AnyAsync(
                employee =>
                    employee.Email == email
                    && (currentEmployeeId == null
                        || employee.Id != currentEmployeeId.Value),
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "Email is already used by another employee.");
        }
    }

    private static string NormalizeRequiredText(
        string value,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{fieldName} cannot be empty.");
        }

        return value.Trim();
    }

    private static string NormalizeEmail(
        string email)
    {
        return NormalizeRequiredText(
                email,
                "Email")
            .ToLowerInvariant();
    }
}
