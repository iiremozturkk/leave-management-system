using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Rules;
using LeaveManagementSystem.Domain.Enums;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.ApproveLeaveRequest;

public sealed class ApproveLeaveRequestCommandHandler(
    ILeaveRequestWriteRepository leaveRequestWriteRepository,
    ILeaveRequestReadRepository leaveRequestReadRepository,
    IEmployeeReadRepository employeeReadRepository)
    : IRequestHandler<
        ApproveLeaveRequestCommand,
        LeaveRequestDto?>
{
    public async Task<LeaveRequestDto?> Handle(
        ApproveLeaveRequestCommand request,
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

        LeaveRequestRules.EnsureSupportedDateRange(
            leaveRequest.StartDate,
            leaveRequest.EndDate);

        var requestedDaysByYear =
            LeaveRequestRules.GetRequestedDaysByYear(
                leaveRequest.StartDate,
                leaveRequest.EndDate);

        var leaveType =
            await leaveRequestWriteRepository.GetLeaveTypeAsync(
                leaveRequest.LeaveTypeId,
                cancellationToken);

        if (leaveType is null)
        {
            throw new InvalidOperationException(
                "Leave type does not exist.");
        }

        foreach (var requestedDaysForYear in requestedDaysByYear)
        {
            var usedDays =
                await leaveRequestWriteRepository.GetApprovedUsedDaysForYearAsync(
                    leaveRequest.EmployeeId,
                    leaveRequest.LeaveTypeId,
                    requestedDaysForYear.Year,
                    excludedLeaveRequestId: leaveRequest.Id,
                    cancellationToken);

            var entitledDays =
                LeaveRequestRules.CalculateEntitledDays(
                    leaveType.DefaultAnnualAllowanceDays,
                    requestedDaysForYear.Year);

            var remainingDays =
                entitledDays - usedDays;

            if (entitledDays <= 0)
            {
                continue;
            }

            if (requestedDaysForYear.Days > remainingDays)
            {
                throw new InvalidOperationException(
                    "Requested leave days exceed the remaining leave balance.");
            }
        }

        leaveRequest.Approve(
            request.ReviewerEmployeeId,
            request.ManagerComment);

        await leaveRequestWriteRepository.SaveChangesAsync(
            cancellationToken);

        return await leaveRequestReadRepository.GetByIdAsync(
            leaveRequest.Id,
            cancellationToken);
    }
}
