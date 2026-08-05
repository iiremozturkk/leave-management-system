using LeaveManagementSystem.Application.Authentication.Abstractions;
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
    IEmployeeReadRepository employeeReadRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IRequestHandler<
        ApproveLeaveRequestCommand,
        LeaveRequestDto?>
{
    public async Task<LeaveRequestDto?> Handle(
        ApproveLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentUserAccess =
            await currentUserAccessService.GetAsync(
                cancellationToken);

        if (currentUserAccess is null
            || currentUserAccess.Role != EmployeeRole.Manager)
        {
            throw new ForbiddenOperationException(
                "Only current active managers can review leave requests.");
        }

        var reviewerEmployeeId =
            currentUserAccess.EmployeeId;

        var leaveRequest =
            await leaveRequestWriteRepository.GetForModificationAsync(
                request.Id,
                cancellationToken);

        if (leaveRequest is null)
        {
            return null;
        }

        var employee =
            await employeeReadRepository.GetByIdAsync(
                leaveRequest.EmployeeId,
                cancellationToken);

        if (employee is null
            || !employee.IsActive
            || employee.ManagerId != reviewerEmployeeId)
        {
            return null;
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
                await leaveRequestWriteRepository
                    .GetApprovedUsedDaysForYearAsync(
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
            reviewerEmployeeId,
            request.ManagerComment);

        await leaveRequestWriteRepository.SaveChangesAsync(
            cancellationToken);

        return await leaveRequestReadRepository.GetByIdAsync(
            leaveRequest.Id,
            cancellationToken);
    }
}
