using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Rules;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.UpdateLeaveRequest;

public sealed class UpdateLeaveRequestCommandHandler(
    ILeaveRequestWriteRepository writeRepository,
    ILeaveRequestReadRepository readRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IRequestHandler<UpdateLeaveRequestCommand, LeaveRequestDto?>
{
    public async Task<LeaveRequestDto?> Handle(
        UpdateLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        var currentUserAccess =
            await currentUserAccessService.GetAsync(
                cancellationToken);

        if (currentUserAccess is null)
        {
            throw new ForbiddenOperationException(
                "Only current active employees can use leave self-service operations.");
        }

        var leaveRequest =
            await writeRepository
                .GetForModificationForEmployeeAsync(
                    request.Id,
                    currentUserAccess.EmployeeId,
                    cancellationToken);

        if (leaveRequest is null)
        {
            return null;
        }

        LeaveRequestRules.EnsureCanBeModified(
            leaveRequest.Status);

        var reason =
            LeaveRequestRules.NormalizeReason(
                request.Reason);

        LeaveRequestRules.EnsureSupportedDateRange(
            request.StartDate,
            request.EndDate);

        var leaveType =
            await LeaveRequestBusinessRules
                .GetLeaveTypeAsync(
                    writeRepository,
                    request.LeaveTypeId,
                    cancellationToken);

        await LeaveRequestBusinessRules
            .EnsureNoOverlapAsync(
                writeRepository,
                leaveRequest.EmployeeId,
                request.StartDate,
                request.EndDate,
                excludedLeaveRequestId: leaveRequest.Id,
                cancellationToken: cancellationToken);

        await LeaveRequestBusinessRules
            .EnsureEnoughBalanceAsync(
                writeRepository,
                leaveRequest.EmployeeId,
                leaveType,
                request.StartDate,
                request.EndDate,
                excludedLeaveRequestId: leaveRequest.Id,
                cancellationToken: cancellationToken);

        leaveRequest.LeaveTypeId =
            request.LeaveTypeId;

        leaveRequest.Reason =
            reason;

        leaveRequest.SetDateRange(
            request.StartDate,
            request.EndDate);

        leaveRequest.UpdatedAtUtc =
            DateTime.UtcNow;

        await writeRepository.SaveChangesAsync(
            cancellationToken);

        return await readRepository.GetByIdAsync(
            leaveRequest.Id,
            cancellationToken);
    }
}
