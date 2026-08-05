using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.Employees.Commands.CreateEmployee;
using LeaveManagementSystem.Application.Employees.Dtos;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_NormalizesPersistsAndReturnsEmployee()
    {
        var departmentId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository();

        var readRepository =
            new FakeEmployeeReadRepository
            {
                ResultFactory = id =>
                    CreateEmployeeDto(
                        id,
                        departmentId,
                        managerId)
            };

        var currentUserAccessService =
            CreateHrCurrentUserAccessService();

        var handler = new CreateEmployeeCommandHandler(
            currentUserAccessService,
            writeRepository,
            readRepository);

        var command = new CreateEmployeeCommand(
            "  Irem  ",
            "  Ozturk  ",
            "  IREM.OZTURK@EXAMPLE.COM  ",
            departmentId,
            managerId,
            EmployeeRole.Employee);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        var addedEmployee =
            Assert.IsType<Employee>(
                writeRepository.AddedEmployee);

        Assert.Equal(
            "Irem",
            addedEmployee.FirstName);

        Assert.Equal(
            "Ozturk",
            addedEmployee.LastName);

        Assert.Equal(
            "irem.ozturk@example.com",
            addedEmployee.Email);

        Assert.Equal(
            departmentId,
            addedEmployee.DepartmentId);

        Assert.Equal(
            managerId,
            addedEmployee.ManagerId);

        Assert.Equal(
            EmployeeRole.Employee,
            addedEmployee.Role);

        Assert.True(addedEmployee.IsActive);

        Assert.Equal(
            departmentId,
            writeRepository.RequestedDepartmentId);

        Assert.Equal(
            managerId,
            writeRepository.RequestedActiveManagerId);

        Assert.Equal(
            "irem.ozturk@example.com",
            writeRepository.RequestedEmail);

        Assert.Null(
            writeRepository.RequestedExcludedEmployeeId);

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
            1,
            writeRepository.AddCallCount);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);

        Assert.Equal(
            addedEmployee.Id,
            readRepository.RequestedId);

        Assert.Equal(
            addedEmployee.Id,
            result.Id);
    }

    [Fact]
    public async Task Handle_WithoutManager_DoesNotCheckManager()
    {
        var departmentId = Guid.NewGuid();

        var writeRepository =
            new FakeEmployeeWriteRepository();

        var readRepository =
            new FakeEmployeeReadRepository
            {
                ResultFactory = id =>
                    CreateEmployeeDto(
                        id,
                        departmentId,
                        managerId: null)
            };

        var currentUserAccessService =
            CreateHrCurrentUserAccessService();

        var handler = new CreateEmployeeCommandHandler(
            currentUserAccessService,
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            departmentId,
            managerId: null);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.NotNull(result);

        Assert.Equal(
            1,
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.AddCallCount);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_DepartmentDoesNotExist_ThrowsAndDoesNotPersist()
    {
        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                DepartmentExistsResult = false
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var currentUserAccessService =
            CreateHrCurrentUserAccessService();

        var handler = new CreateEmployeeCommandHandler(
            currentUserAccessService,
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
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
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.AddCallCount);

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
        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                ActiveManagerExistsResult = false
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var currentUserAccessService =
            CreateHrCurrentUserAccessService();

        var handler = new CreateEmployeeCommandHandler(
            currentUserAccessService,
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            Guid.NewGuid(),
            Guid.NewGuid());

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
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.AddCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ThrowsAndDoesNotPersist()
    {
        var writeRepository =
            new FakeEmployeeWriteRepository
            {
                EmailExistsResult = true
            };

        var readRepository =
            new FakeEmployeeReadRepository();

        var currentUserAccessService =
            CreateHrCurrentUserAccessService();

        var handler = new CreateEmployeeCommandHandler(
            currentUserAccessService,
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
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
            1,
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.AddCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task Handle_CreatedEmployeeCannotBeReloaded_ThrowsAfterSaving()
    {
        var writeRepository =
            new FakeEmployeeWriteRepository();

        var readRepository =
            new FakeEmployeeReadRepository();

        var currentUserAccessService =
            CreateHrCurrentUserAccessService();

        var handler = new CreateEmployeeCommandHandler(
            currentUserAccessService,
            writeRepository,
            readRepository);

        var command = CreateValidCommand(
            Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.Handle(
                    command,
                    CancellationToken.None));

        Assert.Equal(
            "Employee was created but could not be loaded.",
            exception.Message);

        Assert.Equal(
            1,
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            1,
            writeRepository.AddCallCount);

        Assert.Equal(
            1,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            1,
            readRepository.GetByIdCallCount);
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

        var handler = new CreateEmployeeCommandHandler(
            currentUserAccessService,
            writeRepository,
            readRepository);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(
                null!,
                CancellationToken.None));

        Assert.Equal(
            0,
            currentUserAccessService.CallCount);

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
            writeRepository.AddCallCount);

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

        var handler = new CreateEmployeeCommandHandler(
            currentUserAccessService,
            writeRepository,
            readRepository);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    CreateValidCommand(
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
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.AddCallCount);

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

        var handler = new CreateEmployeeCommandHandler(
            currentUserAccessService,
            writeRepository,
            readRepository);

        var exception =
            await Assert.ThrowsAsync<ForbiddenOperationException>(
                () => handler.Handle(
                    CreateValidCommand(
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
            writeRepository.DepartmentExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.ActiveManagerExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.EmailExistsCallCount);

        Assert.Equal(
            0,
            writeRepository.AddCallCount);

        Assert.Equal(
            0,
            writeRepository.SaveChangesCallCount);

        Assert.Equal(
            0,
            readRepository.GetByIdCallCount);
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

    private static CreateEmployeeCommand CreateValidCommand(
        Guid departmentId,
        Guid? managerId = null)
    {
        return new CreateEmployeeCommand(
            "Irem",
            "Ozturk",
            "irem.ozturk@example.com",
            departmentId,
            managerId,
            EmployeeRole.Employee);
    }

    private static EmployeeDto CreateEmployeeDto(
        Guid id,
        Guid departmentId,
        Guid? managerId)
    {
        return new EmployeeDto(
            id,
            "Irem",
            "Ozturk",
            "irem.ozturk@example.com",
            EmployeeRole.Employee,
            true,
            departmentId,
            "Engineering",
            managerId,
            managerId.HasValue
                ? "Manager User"
                : null,
            DateTime.UtcNow,
            null);
    }

    private sealed class FakeCurrentUserAccessService
        : ICurrentUserAccessService
    {
        public CurrentUserAccess? Result { get; init; }

        public int CallCount { get; private set; }

        public Task<CurrentUserAccess?> GetAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class FakeEmployeeWriteRepository
        : IEmployeeWriteRepository
    {
        public bool DepartmentExistsResult { get; init; } = true;

        public bool ActiveManagerExistsResult { get; init; } = true;

        public bool EmailExistsResult { get; init; }

        public Employee? AddedEmployee { get; private set; }

        public Guid RequestedDepartmentId { get; private set; }

        public Guid RequestedActiveManagerId { get; private set; }

        public string? RequestedEmail { get; private set; }

        public Guid? RequestedExcludedEmployeeId { get; private set; }

        public int DepartmentExistsCallCount { get; private set; }

        public int ActiveManagerExistsCallCount { get; private set; }

        public int EmailExistsCallCount { get; private set; }

        public int AddCallCount { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public Task<Employee?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Employee?>(null);
        }

        public Task<bool> DepartmentExistsAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
        {
            DepartmentExistsCallCount++;
            RequestedDepartmentId = departmentId;

            return Task.FromResult(
                DepartmentExistsResult);
        }

        public Task<bool> IsSoleActiveHrAdministratorAsync(
           Guid employeeId,
           CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "IsSoleActiveHrAdministratorAsync should not be called during employee creation.");
        }

        public Task<bool> ActiveManagerExistsAsync(
           Guid managerId,
           CancellationToken cancellationToken = default)
        {
            ActiveManagerExistsCallCount++;
            RequestedActiveManagerId = managerId;

            return Task.FromResult(
                ActiveManagerExistsResult);
        }

        public Task<Guid?> GetManagerIdAsync(
           Guid employeeId,
           CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "GetManagerIdAsync should not be called during employee creation.");
        }

        public Task<bool> HasActiveDirectReportsAsync(
            Guid managerId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "HasActiveDirectReportsAsync should not be called during employee creation.");
        }

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? excludedEmployeeId,
            CancellationToken cancellationToken = default)
        {
            EmailExistsCallCount++;
            RequestedEmail = email;
            RequestedExcludedEmployeeId = excludedEmployeeId;

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

            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmployeeReadRepository
        : IEmployeeReadRepository
    {
        public Func<Guid, EmployeeDto?>? ResultFactory { get; init; }

        public Guid RequestedId { get; private set; }

        public int GetByIdCallCount { get; private set; }

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

            var result = ResultFactory?.Invoke(id);

            return Task.FromResult(result);
        }
    }
}
