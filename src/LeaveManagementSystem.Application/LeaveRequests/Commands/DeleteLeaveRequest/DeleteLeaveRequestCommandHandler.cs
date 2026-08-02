using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Rules;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.DeleteLeaveRequest;

public sealed class DeleteLeaveRequestCommandHandler(
    ILeaveRequestWriteRepository leaveRequestWriteRepository)
    : IRequestHandler<DeleteLeaveRequestCommand, bool>
{
    public async Task<bool> Handle(
        DeleteLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        var leaveRequest =
            await leaveRequestWriteRepository.GetForModificationAsync(
                request.Id,
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
