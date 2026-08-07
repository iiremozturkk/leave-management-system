using LeaveManagementSystem.Application.Authentication.Abstractions;
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
    IEmployeeReadRepository employeeReadRepository,
    ICurrentUserAccessService currentUserAccessService,
    ILeaveRequestNotificationService leaveRequestNotificationService)
    : IRequestHandler<
        RejectLeaveRequestCommand,
        LeaveRequestDto?>
{
    public async Task<LeaveRequestDto?> Handle(
        RejectLeaveRequestCommand request,
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

        leaveRequest.Reject(
            reviewerEmployeeId,
            request.ManagerComment);

        var reviewedAtUtc =
            leaveRequest.ReviewedAtUtc
            ?? throw new InvalidOperationException(
                "Reviewed timestamp was not set.");

        await leaveRequestWriteRepository.SaveChangesAsync(
            cancellationToken);

        await leaveRequestNotificationService.NotifyReviewCompletedAsync(
            leaveRequest.Id,
            leaveRequest.EmployeeId,
            reviewerEmployeeId,
            LeaveRequestStatus.Rejected,
            reviewedAtUtc,
            cancellationToken);

        return await leaveRequestReadRepository.GetByIdAsync(
            leaveRequest.Id,
            cancellationToken);
    }
}
