using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Domain.Enums;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.RejectLeaveRequest;

public sealed class RejectLeaveRequestCommandHandler(
    ILeaveRequestWriteRepository leaveRequestWriteRepository,
    ILeaveRequestReadRepository leaveRequestReadRepository,
    IEmployeeReadRepository employeeReadRepository)
    : IRequestHandler<
        RejectLeaveRequestCommand,
        LeaveRequestDto?>
{
    public async Task<LeaveRequestDto?> Handle(
        RejectLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var leaveRequest =
            await leaveRequestWriteRepository.GetForModificationAsync(
                request.Id,
                cancellationToken);

        if (leaveRequest is null)
        {
            return null;
        }

        if (request.ReviewerEmployeeId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Reviewer employee id cannot be empty.");
        }

        var employee =
            await employeeReadRepository.GetByIdAsync(
                leaveRequest.EmployeeId,
                cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new InvalidOperationException(
                "Employee does not exist or is not active.");
        }

        var reviewer =
            await employeeReadRepository.GetByIdAsync(
                request.ReviewerEmployeeId,
                cancellationToken);

        if (reviewer is null || !reviewer.IsActive)
        {
            throw new InvalidOperationException(
                "Reviewer does not exist or is not active.");
        }

        if (reviewer.Role != EmployeeRole.Manager)
        {
            throw new ForbiddenOperationException(
                "Only managers can review leave requests.");
        }

        if (employee.ManagerId != request.ReviewerEmployeeId)
        {
            throw new ForbiddenOperationException(
                "Only the employee's direct manager can review this leave request.");
        }

        leaveRequest.Reject(
            request.ReviewerEmployeeId,
            request.ManagerComment);

        await leaveRequestWriteRepository.SaveChangesAsync(
            cancellationToken);

        return await leaveRequestReadRepository.GetByIdAsync(
            leaveRequest.Id,
            cancellationToken);
    }
}
