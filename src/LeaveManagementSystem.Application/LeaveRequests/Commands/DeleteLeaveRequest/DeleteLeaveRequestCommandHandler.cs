using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Rules;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.DeleteLeaveRequest;

public sealed class DeleteLeaveRequestCommandHandler(
    ILeaveRequestWriteRepository leaveRequestWriteRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IRequestHandler<DeleteLeaveRequestCommand, bool>
{
    public async Task<bool> Handle(
        DeleteLeaveRequestCommand request,
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
            await leaveRequestWriteRepository
                .GetForModificationForEmployeeAsync(
                    request.Id,
                    currentUserAccess.EmployeeId,
                    cancellationToken);

        if (leaveRequest is null)
        {
            return false;
        }

        LeaveRequestRules.EnsureCanBeModified(
            leaveRequest.Status);

        leaveRequestWriteRepository.Remove(
            leaveRequest);

        await leaveRequestWriteRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}
