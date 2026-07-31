using MediatR;

namespace LeaveManagementSystem.Application.Employees.Commands.DeleteEmployee;

public sealed record DeleteEmployeeCommand(
    Guid Id)
    : IRequest<bool>;
