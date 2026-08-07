using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeaveManagementSystem.Infrastructure.DemoData;

public sealed class DemoDataSeeder(
    AppDbContext dbContext,
    IPasswordHashService passwordHashService,
    IOptions<DemoDataOptions> options)
{
    private const string DepartmentName =
        "Demo Department";

    private const string HrEmail =
        "hr.demo@example.com";

    private const string ManagerEmail =
        "manager.demo@example.com";

    private const string EmployeeEmail =
        "employee.demo@example.com";

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        var demoDataOptions =
            options.Value;

        if (!demoDataOptions.SeedOnStartup)
        {
            return;
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var department =
            await GetOrCreateDepartmentAsync(
                cancellationToken);

        var hr =
            await GetOrCreateEmployeeAsync(
                HrEmail,
                "Demo",
                "HR",
                EmployeeRole.HR,
                department.Id,
                cancellationToken);

        EnsureNoManager(
            hr,
            HrEmail);

        var manager =
            await GetOrCreateEmployeeAsync(
                ManagerEmail,
                "Demo",
                "Manager",
                EmployeeRole.Manager,
                department.Id,
                cancellationToken);

        EnsureNoManager(
            manager,
            ManagerEmail);

        var employee =
            await GetOrCreateEmployeeAsync(
                EmployeeEmail,
                "Demo",
                "Employee",
                EmployeeRole.Employee,
                department.Id,
                cancellationToken);

        EnsureEmployeeManager(
            employee,
            manager.Id);

        await EnsureUserAccountAsync(
            hr.Id,
            demoDataOptions.Password,
            cancellationToken);

        await EnsureUserAccountAsync(
            manager.Id,
            demoDataOptions.Password,
            cancellationToken);

        await EnsureUserAccountAsync(
            employee.Id,
            demoDataOptions.Password,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    private async Task<Department> GetOrCreateDepartmentAsync(
        CancellationToken cancellationToken)
    {
        var department =
            await dbContext.Departments
                .SingleOrDefaultAsync(
                    item =>
                        item.Name == DepartmentName,
                    cancellationToken);

        if (department is not null)
        {
            return department;
        }

        department = new Department
        {
            Name = DepartmentName
        };

        dbContext.Departments.Add(
            department);

        return department;
    }

    private async Task<Employee> GetOrCreateEmployeeAsync(
        string email,
        string firstName,
        string lastName,
        EmployeeRole role,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail =
            email.Trim().ToLowerInvariant();

        var employee =
            await dbContext.Employees
                .SingleOrDefaultAsync(
                    item =>
                        item.Email == normalizedEmail,
                    cancellationToken);

        if (employee is null)
        {
            employee = new Employee
            {
                FirstName = firstName,
                LastName = lastName,
                Email = normalizedEmail,
                DepartmentId = departmentId,
                Role = role,
                IsActive = true
            };

            dbContext.Employees.Add(
                employee);

            return employee;
        }

        if (employee.Role != role)
        {
            throw new InvalidOperationException(
                $"Demo employee '{normalizedEmail}' has an unexpected role.");
        }

        if (employee.DepartmentId != departmentId)
        {
            throw new InvalidOperationException(
                $"Demo employee '{normalizedEmail}' belongs to an unexpected department.");
        }

        if (!employee.IsActive)
        {
            employee.IsActive = true;
            employee.UpdatedAtUtc = DateTime.UtcNow;
        }

        return employee;
    }

    private static void EnsureNoManager(
        Employee employee,
        string email)
    {
        if (employee.ManagerId.HasValue)
        {
            throw new InvalidOperationException(
                $"Demo employee '{email}' has an unexpected manager.");
        }
    }

    private static void EnsureEmployeeManager(
        Employee employee,
        Guid expectedManagerId)
    {
        if (!employee.ManagerId.HasValue)
        {
            employee.ManagerId =
                expectedManagerId;

            employee.UpdatedAtUtc =
                DateTime.UtcNow;

            return;
        }

        if (employee.ManagerId.Value != expectedManagerId)
        {
            throw new InvalidOperationException(
                $"Demo employee '{EmployeeEmail}' has an unexpected manager.");
        }
    }

    private async Task EnsureUserAccountAsync(
        Guid employeeId,
        string password,
        CancellationToken cancellationToken)
    {
        var userAccount =
            await dbContext.UserAccounts
                .SingleOrDefaultAsync(
                    item =>
                        item.EmployeeId == employeeId,
                    cancellationToken);

        if (userAccount is null)
        {
            var passwordHash =
                passwordHashService.HashPassword(
                    password);

            dbContext.UserAccounts.Add(
                new UserAccount(
                    employeeId,
                    passwordHash));

            return;
        }

        var verificationOutcome =
            passwordHashService.VerifyPassword(
                userAccount.PasswordHash,
                password);

        if (verificationOutcome ==
            PasswordVerificationOutcome.Failed)
        {
            throw new InvalidOperationException(
                $"Demo user account for employee '{employeeId}' has an unexpected password.");
        }

        if (!userAccount.IsActive)
        {
            userAccount.Activate();
        }
    }
}
