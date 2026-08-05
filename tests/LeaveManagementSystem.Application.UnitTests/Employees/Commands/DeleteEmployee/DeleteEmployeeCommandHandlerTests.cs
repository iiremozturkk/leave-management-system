using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.Employees.Commands.DeleteEmployee;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.Employees.Commands.DeleteEmployee;

public sealed class DeleteEmployeeCommandHandlerTests
{
    [Fact]
    public async Task Handle_EmployeeExists_SoftDeletesPersistsAndReturnsTrue()
    {
        var employeeId = Guid.NewGuid();

        var employee =
            CreateExistingEmployee(employeeId);

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate = employee
            };

        var handler =
            CreateHandler(
                writeRepository);

        var command =
            new DeleteEmployeeCommand(employeeId);

        var beforeDelete = DateTime.UtcNow;

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        var afterDelete = DateTime.UtcNow;

        Assert.True(result);

        Assert.False(
            employee.IsActive);

        Assert.NotNull(
            employee.UpdatedAtUtc);

        Assert.InRange(
            employee.UpdatedAtUtc!.Value,
            beforeDelete,
            afterDelete);

        Assert.Equal(
            employeeId,
            writeRepository.RequestedEmployeeId);

        Assert.Equal(
             1,
             writeRepository.GetForUpdateCallCount);

        Assert.Equal(
            employeeId,
            writeRepository.RequestedDirectReportsEmployeeId);

        Assert.Equal(
            1,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_EmployeeHasActiveDirectReports_ThrowsAndDoesNotPersist()
    {
        var employeeId = Guid.NewGuid();

        var employee =
            CreateExistingEmployee(employeeId);

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate = employee,
                HasActiveDirectReportsResult = true
            };

        var handler =
            CreateHandler(
                writeRepository);

        var command =
            new DeleteEmployeeCommand(employeeId);

        var exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "An employee with active direct reports cannot be deactivated.",
            exception.Message);

        Assert.True(
            employee.IsActive);

        Assert.Null(
            employee.UpdatedAtUtc);

        Assert.Equal(
            employeeId,
            writeRepository.RequestedDirectReportsEmployeeId);

        Assert.Equal(
            1,
            writeRepository.GetForUpdateCallCount);

        Assert.Equal(
            1,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_EmployeeIsAlreadyInactive_ReturnsTrueWithoutPersisting()
    {
        var employeeId = Guid.NewGuid();

        var employee =
            CreateExistingEmployee(employeeId);

        employee.IsActive = false;

        var originalUpdatedAtUtc =
            new DateTime(
                2026,
                1,
                15,
                12,
                0,
                0,
                DateTimeKind.Utc);

        employee.UpdatedAtUtc =
            originalUpdatedAtUtc;

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate = employee
            };

        var handler =
            CreateHandler(
                writeRepository);

        var command =
            new DeleteEmployeeCommand(employeeId);

        var result =
            await handler.Handle(
                command,
                CancellationToken.None);

        Assert.True(result);

        Assert.False(employee.IsActive);

        Assert.Equal(
            originalUpdatedAtUtc,
            employee.UpdatedAtUtc);

        Assert.Equal(
            1,
            writeRepository.GetForUpdateCallCount);

        Assert.Equal(
            0,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Null(
            writeRepository.RequestedDirectReportsEmployeeId);
    }

    [Fact]
    public async Task Handle_EmployeeDoesNotExist_ReturnsFalseAndDoesNotPersist()
    {
        var employeeId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate = null
            };

        var handler =
            CreateHandler(
                writeRepository);

        var command =
            new DeleteEmployeeCommand(employeeId);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.False(result);

        Assert.Equal(
            employeeId,
            writeRepository.RequestedEmployeeId);

        Assert.Equal(
            1,
            writeRepository.GetForUpdateCallCount);

        Assert.Equal(
            0,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToRepositoryCalls()
    {
        var employeeId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate =
                    CreateExistingEmployee(employeeId)
            };

        var currentUserAccessService =
            CreateHrCurrentUserAccessService();

        var handler =
            CreateHandler(
                writeRepository,
                currentUserAccessService);

        var command =
            new DeleteEmployeeCommand(employeeId);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        await handler.Handle(
            command,
            cancellationToken);

        Assert.Equal(
            cancellationToken,
            currentUserAccessService.RequestedCancellationToken);

        Assert.Equal(
            cancellationToken,
            writeRepository.GetForUpdateCancellationToken);

        Assert.Equal(
            cancellationToken,
            writeRepository.HasActiveDirectReportsCancellationToken);

        Assert.Equal(
            cancellationToken,
            writeRepository.SaveChangesCancellationToken);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsBeforeDependencyCalls()
    {
        var currentUserAccessService =
            CreateHrCurrentUserAccessService();

        var writeRepository =
            new FakeEmployeeWriteRepository();

        var handler = CreateHandler(
            writeRepository,
            currentUserAccessService);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(
                null!,
                CancellationToken.None));

        Assert.Equal(
            0,
            currentUserAccessService.CallCount);

        Assert.Equal(
            0,
            writeRepository.GetForUpdateCallCount);

        Assert.Equal(
            0,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_LastActiveHrAdministrator_ThrowsAndDoesNotPersist()
    {
        var employeeId =
            Guid.NewGuid();

        var employee =
            CreateExistingEmployee(
                employeeId);

        employee.Role =
            EmployeeRole.HR;

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate =
                    employee,

                IsSoleActiveHrAdministratorResult =
                    true
            };

        var transactionManager =
            new FakeEmployeeAdministrationTransactionManager();

        var handler =
            CreateHandler(
                writeRepository,
                transactionManager:
                    transactionManager);

        var exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => handler.Handle(
                    new DeleteEmployeeCommand(
                        employeeId),
                    CancellationToken.None));

        Assert.Equal(
            "The last active HR administrator cannot be deactivated.",
            exception.Message);

        Assert.True(
            employee.IsActive);

        Assert.Equal(
            EmployeeRole.HR,
            employee.Role);

        Assert.Equal(
            1,
            writeRepository
                .IsSoleActiveHrAdministratorCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            transactionManager.Transaction.CommitCallCount);
    }

    [Fact]
    public async Task Handle_CurrentUserAccessMissing_ThrowsForbiddenBeforeRepositoryCalls()
    {
        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result = null
            };

        var writeRepository =
            new FakeEmployeeWriteRepository();

        var handler = CreateHandler(
            writeRepository,
            currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new DeleteEmployeeCommand(
                        Guid.NewGuid()),
                    CancellationToken.None));

        Assert.Equal(
            "Only current active HR employees can administer employees.",
            exception.Message);

        Assert.Equal(
            1,
            currentUserAccessService.CallCount);

        Assert.Equal(
            0,
            writeRepository.GetForUpdateCallCount);

        Assert.Equal(
            0,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);
    }

    [Theory]
    [InlineData(EmployeeRole.Employee)]
    [InlineData(EmployeeRole.Manager)]
    public async Task Handle_CurrentUserIsNotHr_ThrowsForbiddenBeforeRepositoryCalls(
        EmployeeRole role)
    {
        var currentUserAccessService =
            CreateCurrentUserAccessService(
                role);

        var writeRepository =
            new FakeEmployeeWriteRepository();

        var handler = CreateHandler(
            writeRepository,
            currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    new DeleteEmployeeCommand(
                        Guid.NewGuid()),
                    CancellationToken.None));

        Assert.Equal(
            "Only current active HR employees can administer employees.",
            exception.Message);

        Assert.Equal(
            1,
            currentUserAccessService.CallCount);

        Assert.Equal(
            0,
            writeRepository.GetForUpdateCallCount);

        Assert.Equal(
            0,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);
    }

    private static DeleteEmployeeCommandHandler CreateHandler(
        FakeEmployeeWriteRepository writeRepository,
        FakeCurrentUserAccessService? currentUserAccessService = null,
        FakeEmployeeAdministrationTransactionManager?
        transactionManager = null)
    {
        return new DeleteEmployeeCommandHandler(
            currentUserAccessService
                ?? CreateHrCurrentUserAccessService(),
            writeRepository,
            transactionManager
                ?? new FakeEmployeeAdministrationTransactionManager());
    }

    private static FakeCurrentUserAccessService
        CreateHrCurrentUserAccessService()
    {
        return CreateCurrentUserAccessService(
            EmployeeRole.HR);
    }

    private static FakeCurrentUserAccessService
        CreateCurrentUserAccessService(
            EmployeeRole role)
    {
        return new FakeCurrentUserAccessService
        {
            Result = new CurrentUserAccess(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "current.user@example.com",
                role)
        };
    }

    private static Employee CreateExistingEmployee(
        Guid employeeId)
    {
        return new Employee
        {
            Id = employeeId,
            FirstName = "Irem",
            LastName = "Ozturk",
            Email = "irem.ozturk@example.com",
            DepartmentId = Guid.NewGuid(),
            ManagerId = null,
            Role = EmployeeRole.Employee,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = null
        };
    }

    private sealed class FakeCurrentUserAccessService
    : ICurrentUserAccessService
    {
        public CurrentUserAccess? Result { get; init; }

        public int CallCount { get; private set; }

        public CancellationToken RequestedCancellationToken
        {
            get;
            private set;
        }

        public Task<CurrentUserAccess?> GetAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            RequestedCancellationToken =
                cancellationToken;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class FakeEmployeeWriteRepository
        : IEmployeeWriteRepository
    {
        public Employee? EmployeeForUpdate { get; init; }

        public bool HasActiveDirectReportsResult { get; init; }

        public Guid RequestedEmployeeId { get; private set; }

        public Guid? RequestedDirectReportsEmployeeId
        {
            get;
            private set;
        }

        public int GetForUpdateCallCount { get; private set; }

        public int HasActiveDirectReportsCallCount
        {
            get;
            private set;
        }

        public int SaveChangesCallCount { get; private set; }

        public CancellationToken GetForUpdateCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken HasActiveDirectReportsCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken SaveChangesCancellationToken
        {
            get;
            private set;
        }

        public bool IsSoleActiveHrAdministratorResult
        {
            get;
            init;
        }

        public int IsSoleActiveHrAdministratorCallCount
        {
            get;
            private set;
        }

        public Task<Employee?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            GetForUpdateCallCount++;
            RequestedEmployeeId = id;
            GetForUpdateCancellationToken =
                cancellationToken;

            return Task.FromResult(
                EmployeeForUpdate);
        }

        public Task<bool> DepartmentExistsAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "DepartmentExistsAsync should not be called during employee deletion.");
        }

        public Task<bool> ActiveManagerExistsAsync(
            Guid managerId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "ActiveManagerExistsAsync should not be called during employee deletion.");
        }

        public Task<bool> IsSoleActiveHrAdministratorAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            IsSoleActiveHrAdministratorCallCount++;

            return Task.FromResult(
                IsSoleActiveHrAdministratorResult);
        }
        public Task<Guid?> GetManagerIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "GetManagerIdAsync should not be called during employee deletion.");
        }

        public Task<bool> HasActiveDirectReportsAsync(
            Guid managerId,
            CancellationToken cancellationToken = default)
        {
            HasActiveDirectReportsCallCount++;
            RequestedDirectReportsEmployeeId =
                managerId;

            HasActiveDirectReportsCancellationToken =
                cancellationToken;

            return Task.FromResult(
                HasActiveDirectReportsResult);
        }

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? excludedEmployeeId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "EmailExistsAsync should not be called during employee deletion.");
        }

        public void Add(
            Employee employee)
        {
            throw new InvalidOperationException(
                "Add should not be called during employee deletion.");
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            SaveChangesCancellationToken =
                cancellationToken;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmployeeAdministrationTransactionManager
        : IEmployeeAdministrationTransactionManager
    {
        public FakeEmployeeAdministrationTransaction Transaction
        {
            get;
        } = new();

        public Task<IEmployeeAdministrationTransaction> BeginAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEmployeeAdministrationTransaction>(
                Transaction);
        }
    }

    private sealed class FakeEmployeeAdministrationTransaction
        : IEmployeeAdministrationTransaction
    {
        public int CommitCallCount
        {
            get;
            private set;
        }

        public Task CommitAsync(
            CancellationToken cancellationToken = default)
        {
            CommitCallCount++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
