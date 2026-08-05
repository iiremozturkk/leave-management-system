using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.Employees.Commands.UpdateEmployee;
using LeaveManagementSystem.Application.Employees.Dtos;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_NormalizesUpdatesPersistsAndReturnsEmployee()
    {
        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var employee =
            CreateExistingEmployee(employeeId);

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate = employee
            };

        var readRepository =
            new FakeEmployeeReadRepository
            {
                ResultFactory = id =>
                    CreateEmployeeDto(
                        id,
                        departmentId,
                        managerId,
                        firstName: "Irem",
                        lastName: "Ozturk",
                        email: "irem.ozturk@example.com",
                        role: EmployeeRole.Manager,
                        isActive: false)
            };

        var handler = CreateHandler(
            writeRepository,
            readRepository);

        var command = new UpdateEmployeeCommand(
            employeeId,
            "  Irem  ",
            "  Ozturk  ",
            "  IREM.OZTURK@EXAMPLE.COM  ",
            departmentId,
            managerId,
            EmployeeRole.Manager,
            IsActive: false);

        var beforeUpdate = DateTime.UtcNow;

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        var afterUpdate = DateTime.UtcNow;

        Assert.NotNull(result);

        Assert.Equal(
            "Irem",
            employee.FirstName);

        Assert.Equal(
            "Ozturk",
            employee.LastName);

        Assert.Equal(
            "irem.ozturk@example.com",
            employee.Email);

        Assert.Equal(
            departmentId,
            employee.DepartmentId);

        Assert.Equal(
            managerId,
            employee.ManagerId);

        Assert.Equal(
            EmployeeRole.Manager,
            employee.Role);

        Assert.False(employee.IsActive);

        Assert.NotNull(
            employee.UpdatedAtUtc);

        Assert.InRange(
            employee.UpdatedAtUtc!.Value,
            beforeUpdate,
            afterUpdate);

        Assert.Equal(
            employeeId,
            writeRepository.RequestedEmployeeId);

        Assert.Equal(
            departmentId,
            writeRepository.RequestedDepartmentId);

        Assert.Equal(
            managerId,
            writeRepository.RequestedActiveManagerId);

        Assert.Equal(
            "irem.ozturk@example.com",
            writeRepository.RequestedEmail);

        Assert.Equal(
            employeeId,
            writeRepository.RequestedExcludedEmployeeId);

        Assert.Equal(
            1,
            writeRepository.GetForUpdateCallCount);

        Assert.Equal(
            1,
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.AddCallCount);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);

        Assert.Equal(
            employeeId,
            readRepository.RequestedId);

        Assert.Equal(
            employeeId,
            result!.Id);
    }

    [Fact]
    public async Task Handle_EmployeeDoesNotExist_ReturnsNullAndDoesNotContinue()
    {
        var employeeId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate = null
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler = CreateHandler(
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            employeeId,
            Guid.NewGuid());

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.Null(result);

        Assert.Equal(
            1,
            writeRepository.GetForUpdateCallCount);

        Assert.Equal(
            0,
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_DepartmentDoesNotExist_ThrowsAndDoesNotPersist()
    {
        var employeeId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate =
                    CreateExistingEmployee(employeeId),
                DepartmentExistsResult = false
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler = CreateHandler(
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            employeeId,
            Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Department does not exist.",
            exception.Message);

        Assert.Equal(
            1,
            writeRepository.GetForUpdateCallCount);

        Assert.Equal(
            1,
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_EmployeeIsOwnManager_ThrowsWithoutCheckingManager()
    {
        var employeeId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate =
                    CreateExistingEmployee(employeeId)
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler = CreateHandler(
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            employeeId,
            Guid.NewGuid(),
            managerId: employeeId);

        var exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "An employee cannot be their own manager.",
            exception.Message);

        Assert.Equal(
            1,
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_ManagerIsNotAnActiveManager_ThrowsAndDoesNotPersist()
    {
        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate =
                    CreateExistingEmployee(employeeId),
                ActiveManagerExistsResult = false
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler = CreateHandler(
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            employeeId,
            Guid.NewGuid(),
            managerId);

        var exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Manager does not exist, is not active, or does not have the Manager role.",
            exception.Message);

        Assert.Equal(
            1,
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            managerId,
            writeRepository.RequestedActiveManagerId);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_ProposedManagerCreatesIndirectCycle_ThrowsAndDoesNotPersist()
    {
        var employeeId = Guid.NewGuid();
        var proposedManagerId = Guid.NewGuid();
        var upperManagerId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate =
                    CreateExistingEmployee(employeeId),

                ManagerIdResultFactory = id =>
                {
                    if (id == proposedManagerId)
                    {
                        return upperManagerId;
                    }

                    if (id == upperManagerId)
                    {
                        return employeeId;
                    }

                    return null;
                }
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler =
            CreateHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
                employeeId,
                Guid.NewGuid(),
                proposedManagerId);

        var exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Manager hierarchy cannot contain a cycle.",
            exception.Message);

        Assert.Equal(
            1,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            2,
            writeRepository.GetManagerIdCallCount);

        Assert.Equal(
            0,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_DeactivatingManagerWithActiveDirectReports_ThrowsAndDoesNotPersist()
    {
        var employeeId = Guid.NewGuid();

        var employee =
            CreateExistingEmployee(employeeId);

        employee.Role =
            EmployeeRole.Manager;

        employee.IsActive =
            true;

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate = employee,
                HasActiveDirectReportsResult = true
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler =
            CreateHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
                employeeId,
                Guid.NewGuid(),
                null)
            with
            {
                Role = EmployeeRole.Manager,
                IsActive = false
            };

        var exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "A manager with active direct reports cannot be deactivated or assigned another role.",
            exception.Message);

        Assert.Equal(
            1,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.GetManagerIdCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);

        Assert.True(employee.IsActive);

        Assert.Equal(
            EmployeeRole.Manager,
            employee.Role);
    }

    [Fact]
    public async Task Handle_DemotingManagerWithActiveDirectReports_ThrowsAndDoesNotPersist()
    {
        var employeeId = Guid.NewGuid();

        var employee =
            CreateExistingEmployee(employeeId);

        employee.Role =
            EmployeeRole.Manager;

        employee.IsActive =
            true;

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate = employee,
                HasActiveDirectReportsResult = true
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler =
            CreateHandler(
                writeRepository,
                readRepository);

        var command =
            CreateValidCommand(
                employeeId,
                Guid.NewGuid(),
                null)
            with
            {
                Role = EmployeeRole.Employee,
                IsActive = true
            };

        var exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "A manager with active direct reports cannot be deactivated or assigned another role.",
            exception.Message);

        Assert.Equal(
            1,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.GetManagerIdCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);

        Assert.True(employee.IsActive);

        Assert.Equal(
            EmployeeRole.Manager,
            employee.Role);

        Assert.Null(
            employee.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_WithoutManager_DoesNotCheckManager()
    {
        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate =
                    CreateExistingEmployee(employeeId)
            };

        var readRepository =
            new FakeEmployeeReadRepository
            {
                ResultFactory = id =>
                    CreateEmployeeDto(
                        id,
                        departmentId,
                        managerId: null)
            };

        var handler = CreateHandler(
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            employeeId,
            departmentId,
            managerId: null);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.NotNull(result);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_ManagerIsRemoved_ClearsExistingManagerWithoutCheckingManager()
    {
        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        var employee =
            CreateExistingEmployee(employeeId);

        employee.ManagerId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate = employee
            };

        var readRepository =
            new FakeEmployeeReadRepository
            {
                ResultFactory = id =>
                    CreateEmployeeDto(
                        id,
                        departmentId,
                        managerId: null)
            };

        var handler = CreateHandler(
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            employeeId,
            departmentId,
            managerId: null);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.NotNull(result);

        Assert.Null(
            employee.ManagerId);

        Assert.Null(
            result!.ManagerId);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ThrowsAndDoesNotPersist()
    {
        var employeeId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate =
                    CreateExistingEmployee(employeeId),
                EmailExistsResult = true
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler = CreateHandler(
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            employeeId,
            Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Email is already used by another employee.",
            exception.Message);

        Assert.Equal(
            "irem.ozturk@example.com",
            writeRepository.RequestedEmail);

        Assert.Equal(
            employeeId,
            writeRepository.RequestedExcludedEmployeeId);

        Assert.Equal(
            1,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_CurrentEmployeeEmail_PassesEmployeeIdToUniquenessCheckAndPersists()
    {
        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        var employee =
            CreateExistingEmployee(employeeId);

        employee.Email =
            "irem.ozturk@example.com";

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate = employee,
                EmailExistsResult = false
            };

        var readRepository =
            new FakeEmployeeReadRepository
            {
                ResultFactory = id =>
                    CreateEmployeeDto(
                        id,
                        departmentId,
                        managerId: null)
            };

        var handler = CreateHandler(
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            employeeId,
            departmentId) with
        {
            Email = "  IREM.OZTURK@EXAMPLE.COM  "
        };

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.NotNull(result);

        Assert.Equal(
            "irem.ozturk@example.com",
            writeRepository.RequestedEmail);

        Assert.Equal(
            employeeId,
            writeRepository.RequestedExcludedEmployeeId);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_UpdatedEmployeeCannotBeReloaded_ThrowsAfterSaving()
    {
        var employeeId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate =
                    CreateExistingEmployee(employeeId)
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler = CreateHandler(
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            employeeId,
            Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Employee was updated but could not be loaded.",
            exception.Message);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);

        Assert.Equal(
            employeeId,
            readRepository.RequestedId);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToAllRepositoryCalls()
    {
        var currentUserAccessService =
            CreateHrCurrentUserAccessService();

        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmployeeForUpdate =
                    CreateExistingEmployee(employeeId)
            };

        var readRepository =
            new FakeEmployeeReadRepository
            {
                ResultFactory = id =>
                    CreateEmployeeDto(
                        id,
                        departmentId,
                        managerId)
            };

        var handler = CreateHandler(
            writeRepository,
            readRepository,
            currentUserAccessService);

        var command = CreateValidCommand(
            employeeId,
            departmentId,
            managerId);

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
            writeRepository.DepartmentExistsCancellationToken);

        Assert.Equal(
            cancellationToken,
            writeRepository.ActiveManagerExistsCancellationToken);

        Assert.Equal(
            cancellationToken,
            writeRepository.GetManagerIdCancellationToken);

        Assert.Equal(
            cancellationToken,
            writeRepository.EmailExistsCancellationToken);

        Assert.Equal(
            cancellationToken,
            writeRepository.SaveChangesCancellationToken);

        Assert.Equal(
            cancellationToken,
            readRepository.GetByIdCancellationToken);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsBeforeDependencyCalls()
    {
        var currentUserAccessService =
            CreateHrCurrentUserAccessService();

        var writeRepository =
            new FakeEmployeeWriteRepository();

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler = CreateHandler(
            writeRepository,
            readRepository,
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
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.GetManagerIdCallCount);

        Assert.Equal(
            0,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
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

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler = CreateHandler(
            writeRepository,
            readRepository,
            currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    CreateValidCommand(
                        Guid.NewGuid(),
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
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.GetManagerIdCallCount);

        Assert.Equal(
            0,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
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

        var readRepository =
            new FakeEmployeeReadRepository();

        var handler = CreateHandler(
            writeRepository,
            readRepository,
            currentUserAccessService);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    CreateValidCommand(
                        Guid.NewGuid(),
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
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.GetManagerIdCallCount);

        Assert.Equal(
            0,
            writeRepository.HasActiveDirectReportsCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    private static UpdateEmployeeCommandHandler CreateHandler(
        FakeEmployeeWriteRepository writeRepository,
        FakeEmployeeReadRepository readRepository,
        FakeCurrentUserAccessService? currentUserAccessService = null)
    {
        return new UpdateEmployeeCommandHandler(
            currentUserAccessService
                ?? CreateHrCurrentUserAccessService(),
            writeRepository,
            readRepository);
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

    private static UpdateEmployeeCommand CreateValidCommand(
        Guid employeeId,
        Guid departmentId,
        Guid? managerId = null)
    {
        return new UpdateEmployeeCommand(
            employeeId,
            "Irem",
            "Ozturk",
            "irem.ozturk@example.com",
            departmentId,
            managerId,
            EmployeeRole.Employee,
            IsActive: true);
    }

    private static Employee CreateExistingEmployee(
        Guid employeeId)
    {
        return new Employee
        {
            Id = employeeId,
            FirstName = "Old",
            LastName = "Employee",
            Email = "old.employee@example.com",
            DepartmentId = Guid.NewGuid(),
            ManagerId = null,
            Role = EmployeeRole.Employee,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = null
        };
    }

    private static EmployeeDto CreateEmployeeDto(
        Guid id,
        Guid departmentId,
        Guid? managerId,
        string firstName = "Irem",
        string lastName = "Ozturk",
        string email = "irem.ozturk@example.com",
        EmployeeRole role = EmployeeRole.Employee,
        bool isActive = true)
    {
        return new EmployeeDto(
            id,
            firstName,
            lastName,
            email,
            role,
            isActive,
            departmentId,
            "Engineering",
            managerId,
            managerId.HasValue
                ? "Manager User"
                : null,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow);
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

        public bool DepartmentExistsResult { get; init; } = true;

        public bool ActiveManagerExistsResult { get; init; } = true;

        public Func<Guid, Guid?>? ManagerIdResultFactory { get; init; }

        public bool HasActiveDirectReportsResult { get; init; }

        public bool EmailExistsResult { get; init; }

        public Employee? AddedEmployee { get; private set; }

        public Guid RequestedEmployeeId { get; private set; }

        public Guid RequestedDepartmentId { get; private set; }

        public Guid RequestedActiveManagerId { get; private set; }

        public string? RequestedEmail { get; private set; }

        public Guid? RequestedExcludedEmployeeId { get; private set; }

        public int GetForUpdateCallCount { get; private set; }

        public int GetManagerIdCallCount { get; private set; }

        public int HasActiveDirectReportsCallCount { get; private set; }

        public int DepartmentExistsCallCount { get; private set; }

        public int ActiveManagerExistsCallCount { get; private set; }

        public int EmailExistsCallCount { get; private set; }

        public int AddCallCount { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public CancellationToken GetForUpdateCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken DepartmentExistsCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken ActiveManagerExistsCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken GetManagerIdCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken HasActiveDirectReportsCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken EmailExistsCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken SaveChangesCancellationToken
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
            DepartmentExistsCallCount++;
            RequestedDepartmentId = departmentId;
            DepartmentExistsCancellationToken =
                cancellationToken;

            return Task.FromResult(
                DepartmentExistsResult);
        }

        public Task<bool> ActiveManagerExistsAsync(
           Guid managerId,
           CancellationToken cancellationToken = default)
        {
            ActiveManagerExistsCallCount++;
            RequestedActiveManagerId = managerId;
            ActiveManagerExistsCancellationToken =
                cancellationToken;

            return Task.FromResult(
                ActiveManagerExistsResult);
        }

        public Task<Guid?> GetManagerIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            GetManagerIdCallCount++;
            GetManagerIdCancellationToken =
                cancellationToken;

            var managerId =
                ManagerIdResultFactory?.Invoke(employeeId);

            return Task.FromResult(managerId);
        }

        public Task<bool> HasActiveDirectReportsAsync(
            Guid managerId,
            CancellationToken cancellationToken = default)
        {
            HasActiveDirectReportsCallCount++;
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
            EmailExistsCallCount++;
            RequestedEmail = email;
            RequestedExcludedEmployeeId =
                excludedEmployeeId;
            EmailExistsCancellationToken =
                cancellationToken;

            return Task.FromResult(
                EmailExistsResult);
        }

        public void Add(Employee employee)
        {
            AddCallCount++;
            AddedEmployee = employee;
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

    private sealed class FakeEmployeeReadRepository
        : IEmployeeReadRepository
    {
        public Func<Guid, EmployeeDto?>? ResultFactory { get; init; }

        public Guid RequestedId { get; private set; }

        public int GetByIdCallCount { get; private set; }

        public CancellationToken GetByIdCancellationToken
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<EmployeeDto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<EmployeeDto>>(
                Array.Empty<EmployeeDto>());
        }

        public Task<EmployeeDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            RequestedId = id;
            GetByIdCancellationToken =
                cancellationToken;

            var result = ResultFactory?.Invoke(id);

            return Task.FromResult(result);
        }
    }
}
