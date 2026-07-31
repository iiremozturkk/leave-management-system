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
            new DeleteEmployeeCommandHandler(
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
            1,
            writeRepository.SaveChangesCallCount);
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
            new DeleteEmployeeCommandHandler(
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

        var handler =
            new DeleteEmployeeCommandHandler(
                writeRepository);

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
            writeRepository.GetForUpdateCancellationToken);

        Assert.Equal(
            cancellationToken,
            writeRepository.SaveChangesCancellationToken);
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

    private sealed class FakeEmployeeWriteRepository
        : IEmployeeWriteRepository
    {
        public Employee? EmployeeForUpdate { get; init; }

        public Guid RequestedEmployeeId { get; private set; }

        public int GetForUpdateCallCount { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public CancellationToken GetForUpdateCancellationToken
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
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public Task<bool> ActiveEmployeeExistsAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? excludedEmployeeId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
        }

        public void Add(
            Employee employee)
        {
            throw new InvalidOperationException(
                "Unexpected repository call.");
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
}
