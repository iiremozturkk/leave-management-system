using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.Authentication.Persistence;

public sealed class CurrentUserAccessReadRepositoryTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetByUserAccountIdAsync_WhenAccountExists_ReturnsCurrentAccessData()
    {
        await EnsureDatabaseReadyAsync();

        var testData =
            await CreateTestUserAsync(
                EmployeeRole.Manager,
                isUserAccountActive: true,
                isEmployeeActive: true);

        try
        {
            using var scope =
                _factory.Services.CreateScope();

            var repository =
                scope.ServiceProvider
                    .GetRequiredService<
                        ICurrentUserAccessReadRepository>();

            var result =
                await repository.GetByUserAccountIdAsync(
                    testData.UserAccountId);

            Assert.NotNull(
                result);

            Assert.Equal(
                testData.UserAccountId,
                result.UserAccountId);

            Assert.Equal(
                testData.EmployeeId,
                result.EmployeeId);

            Assert.Equal(
                testData.Email,
                result.Email);

            Assert.Equal(
                EmployeeRole.Manager,
                result.Role);

            Assert.True(
                result.IsUserAccountActive);

            Assert.True(
                result.IsEmployeeActive);
        }
        finally
        {
            await CleanupTestUserAsync(
                testData);
        }
    }

    [Fact]
    public async Task GetByUserAccountIdAsync_WhenAccountDoesNotExist_ReturnsNull()
    {
        await EnsureDatabaseReadyAsync();

        using var scope =
            _factory.Services.CreateScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<
                    ICurrentUserAccessReadRepository>();

        var result =
            await repository.GetByUserAccountIdAsync(
                Guid.NewGuid());

        Assert.Null(
            result);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task GetByUserAccountIdAsync_ReturnsCurrentActiveStates(
        bool isUserAccountActive,
        bool isEmployeeActive)
    {
        await EnsureDatabaseReadyAsync();

        var testData =
            await CreateTestUserAsync(
                EmployeeRole.HR,
                isUserAccountActive,
                isEmployeeActive);

        try
        {
            using var scope =
                _factory.Services.CreateScope();

            var repository =
                scope.ServiceProvider
                    .GetRequiredService<
                        ICurrentUserAccessReadRepository>();

            var result =
                await repository.GetByUserAccountIdAsync(
                    testData.UserAccountId);

            Assert.NotNull(
                result);

            Assert.Equal(
                EmployeeRole.HR,
                result.Role);

            Assert.Equal(
                isUserAccountActive,
                result.IsUserAccountActive);

            Assert.Equal(
                isEmployeeActive,
                result.IsEmployeeActive);
        }
        finally
        {
            await CleanupTestUserAsync(
                testData);
        }
    }

    [Fact]
    public async Task GetByUserAccountIdAsync_WithCanceledToken_ThrowsOperationCanceledException()
    {
        await EnsureDatabaseReadyAsync();

        using var scope =
            _factory.Services.CreateScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<
                    ICurrentUserAccessReadRepository>();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByUserAccountIdAsync(
                Guid.NewGuid(),
                cancellationTokenSource.Token));
    }

    private async Task<TestUserData> CreateTestUserAsync(
        EmployeeRole role,
        bool isUserAccountActive,
        bool isEmployeeActive)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var departmentId =
            Guid.NewGuid();

        var employeeId =
            Guid.NewGuid();

        var suffix =
            Guid.NewGuid().ToString("N");

        var email =
            $"current.user.access.{suffix}@example.com";

        var department =
            new Department
            {
                Id =
                    departmentId,

                Name =
                    $"Current User Access Department {suffix}",

                CreatedAtUtc =
                    DateTime.UtcNow,

                UpdatedAtUtc =
                    null
            };

        var employee =
            new Employee
            {
                Id =
                    employeeId,

                FirstName =
                    "Current",

                LastName =
                    "AccessUser",

                Email =
                    email,

                DepartmentId =
                    departmentId,

                ManagerId =
                    null,

                Role =
                    role,

                IsActive =
                    isEmployeeActive,

                CreatedAtUtc =
                    DateTime.UtcNow,

                UpdatedAtUtc =
                    null
            };

        var userAccount =
            new UserAccount(
                employeeId,
                "current-user-access-test-password-hash");

        if (!isUserAccountActive)
        {
            userAccount.Deactivate();
        }

        dbContext.Departments.Add(
            department);

        dbContext.Employees.Add(
            employee);

        dbContext.UserAccounts.Add(
            userAccount);

        await dbContext.SaveChangesAsync();

        return new TestUserData(
            userAccount.Id,
            employeeId,
            departmentId,
            email);
    }

    private async Task CleanupTestUserAsync(
        TestUserData testData)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var userAccount =
            await dbContext.UserAccounts
                .FirstOrDefaultAsync(
                    account =>
                        account.Id ==
                        testData.UserAccountId);

        if (userAccount is not null)
        {
            dbContext.UserAccounts.Remove(
                userAccount);

            await dbContext.SaveChangesAsync();
        }

        var employee =
            await dbContext.Employees
                .FirstOrDefaultAsync(
                    employee =>
                        employee.Id ==
                        testData.EmployeeId);

        if (employee is not null)
        {
            dbContext.Employees.Remove(
                employee);

            await dbContext.SaveChangesAsync();
        }

        var department =
            await dbContext.Departments
                .FirstOrDefaultAsync(
                    department =>
                        department.Id ==
                        testData.DepartmentId);

        if (department is not null)
        {
            dbContext.Departments.Remove(
                department);

            await dbContext.SaveChangesAsync();
        }
    }

    private sealed record TestUserData(
        Guid UserAccountId,
        Guid EmployeeId,
        Guid DepartmentId,
        string Email);
}
