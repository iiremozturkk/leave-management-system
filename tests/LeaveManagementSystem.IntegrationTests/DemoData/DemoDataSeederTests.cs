using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.DemoData;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LeaveManagementSystem.IntegrationTests.DemoData;

public sealed class DemoDataSeederTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    private const string DepartmentName = "Demo Department";
    private const string HrEmail = "hr.demo@example.com";
    private const string ManagerEmail = "manager.demo@example.com";
    private const string EmployeeEmail = "employee.demo@example.com";
    private const string Password = "Demo-Integration-Test-Password-123!";

    private static readonly string[] DemoEmails =
    {
        HrEmail,
        ManagerEmail,
        EmployeeEmail
    };

    [Fact]
    public async Task SeedAsync_WhenRunTwice_CreatesExpectedDemoDataOnlyOnce()
    {
        await EnsureDatabaseReadyAsync();
        await CleanupDemoDataAsync();

        try
        {
            using var scope = _factory.Services.CreateScope();

            var dbContext =
                scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var passwordHashService =
                scope.ServiceProvider.GetRequiredService<IPasswordHashService>();

            var seeder =
                new DemoDataSeeder(
                    dbContext,
                    passwordHashService,
                    Options.Create(
                        new DemoDataOptions
                        {
                            SeedOnStartup = true,
                            Password = Password
                        }));

            await seeder.SeedAsync();
            dbContext.ChangeTracker.Clear();

            var firstDepartment =
                await dbContext.Departments
                    .AsNoTracking()
                    .SingleAsync(
                        department =>
                            department.Name == DepartmentName);

            var firstEmployees =
                await dbContext.Employees
                    .AsNoTracking()
                    .Where(
                        employee =>
                            DemoEmails.Contains(employee.Email))
                    .ToListAsync();

            AssertExpectedEmployees(
                firstEmployees,
                firstDepartment.Id);

            var firstEmployeeIds =
                firstEmployees
                    .Select(employee => employee.Id)
                    .ToArray();

            var firstAccounts =
                await dbContext.UserAccounts
                    .AsNoTracking()
                    .Where(
                        account =>
                            firstEmployeeIds.Contains(account.EmployeeId))
                    .ToListAsync();

            AssertExpectedAccounts(
                firstAccounts,
                firstEmployeeIds,
                passwordHashService);

            var firstEmployeeSnapshot =
                firstEmployees.ToDictionary(
                    employee => employee.Email,
                    employee =>
                        (
                            Id: employee.Id,
                            UpdatedAtUtc: employee.UpdatedAtUtc
                        ));

            var firstAccountSnapshot =
                firstAccounts.ToDictionary(
                    account => account.EmployeeId,
                    account =>
                        (
                            Id: account.Id,
                            PasswordHash: account.PasswordHash,
                            UpdatedAtUtc: account.UpdatedAtUtc
                        ));

            var firstDepartmentId =
                firstDepartment.Id;

            var firstDepartmentUpdatedAtUtc =
                firstDepartment.UpdatedAtUtc;

            await seeder.SeedAsync();
            dbContext.ChangeTracker.Clear();

            var secondDepartment =
                await dbContext.Departments
                    .AsNoTracking()
                    .SingleAsync(
                        department =>
                            department.Name == DepartmentName);

            var secondEmployees =
                await dbContext.Employees
                    .AsNoTracking()
                    .Where(
                        employee =>
                            DemoEmails.Contains(employee.Email))
                    .ToListAsync();

            AssertExpectedEmployees(
                secondEmployees,
                secondDepartment.Id);

            var secondEmployeeIds =
                secondEmployees
                    .Select(employee => employee.Id)
                    .ToArray();

            var secondAccounts =
                await dbContext.UserAccounts
                    .AsNoTracking()
                    .Where(
                        account =>
                            secondEmployeeIds.Contains(account.EmployeeId))
                    .ToListAsync();

            AssertExpectedAccounts(
                secondAccounts,
                secondEmployeeIds,
                passwordHashService);

            Assert.Equal(
                firstDepartmentId,
                secondDepartment.Id);

            Assert.Equal(
                firstDepartmentUpdatedAtUtc,
                secondDepartment.UpdatedAtUtc);

            foreach (var employee in secondEmployees)
            {
                var firstState =
                    firstEmployeeSnapshot[employee.Email];

                Assert.Equal(
                    firstState.Id,
                    employee.Id);

                Assert.Equal(
                    firstState.UpdatedAtUtc,
                    employee.UpdatedAtUtc);
            }

            foreach (var account in secondAccounts)
            {
                var firstState =
                    firstAccountSnapshot[account.EmployeeId];

                Assert.Equal(
                    firstState.Id,
                    account.Id);

                Assert.Equal(
                    firstState.PasswordHash,
                    account.PasswordHash);

                Assert.Equal(
                    firstState.UpdatedAtUtc,
                    account.UpdatedAtUtc);
            }
        }
        finally
        {
            await CleanupDemoDataAsync();
        }
    }

    private static void AssertExpectedEmployees(
        IReadOnlyList<Employee> employees,
        Guid departmentId)
    {
        Assert.Equal(
            3,
            employees.Count);

        var hr =
            employees.Single(
                employee =>
                    employee.Email == HrEmail);

        var manager =
            employees.Single(
                employee =>
                    employee.Email == ManagerEmail);

        var employee =
            employees.Single(
                item =>
                    item.Email == EmployeeEmail);

        Assert.Equal(
            EmployeeRole.HR,
            hr.Role);

        Assert.Null(
            hr.ManagerId);

        Assert.Equal(
            EmployeeRole.Manager,
            manager.Role);

        Assert.Null(
            manager.ManagerId);

        Assert.Equal(
            EmployeeRole.Employee,
            employee.Role);

        Assert.Equal(
            manager.Id,
            employee.ManagerId);

        Assert.All(
            employees,
            item =>
            {
                Assert.True(
                    item.IsActive);

                Assert.Equal(
                    departmentId,
                    item.DepartmentId);
            });
    }

    private static void AssertExpectedAccounts(
        IReadOnlyList<UserAccount> accounts,
        IReadOnlyCollection<Guid> employeeIds,
        IPasswordHashService passwordHashService)
    {
        Assert.Equal(
            3,
            accounts.Count);

        Assert.All(
            accounts,
            account =>
            {
                Assert.Contains(
                    account.EmployeeId,
                    employeeIds);

                Assert.True(
                    account.IsActive);

                var verificationOutcome =
                    passwordHashService.VerifyPassword(
                        account.PasswordHash,
                        Password);

                Assert.NotEqual(
                    PasswordVerificationOutcome.Failed,
                    verificationOutcome);
            });
    }

    private async Task CleanupDemoDataAsync()
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Employees
            .Where(
                employee =>
                    employee.Email == EmployeeEmail)
            .ExecuteDeleteAsync();

        await dbContext.Employees
            .Where(
                employee =>
                    employee.Email == ManagerEmail)
            .ExecuteDeleteAsync();

        await dbContext.Employees
            .Where(
                employee =>
                    employee.Email == HrEmail)
            .ExecuteDeleteAsync();

        await dbContext.Departments
            .Where(
                department =>
                    department.Name == DepartmentName)
            .ExecuteDeleteAsync();
    }
}

