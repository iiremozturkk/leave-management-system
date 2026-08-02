using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Rules;
using LeaveManagementSystem.Domain.Entities;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.CreateLeaveRequest;

public sealed class CreateLeaveRequestCommandHandler(
    ILeaveRequestWriteRepository writeRepository,
    ILeaveRequestReadRepository readRepository)
    : IRequestHandler<CreateLeaveRequestCommand, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(
        CreateLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        var reason =
            LeaveRequestRules.NormalizeReason(
                request.Reason);

        LeaveRequestRules.EnsureSupportedDateRange(
            request.StartDate,
            request.EndDate);

        await LeaveRequestBusinessRules
            .EnsureActiveEmployeeExistsAsync(
                writeRepository,
                request.EmployeeId,
                cancellationToken);

        var leaveType =
            await LeaveRequestBusinessRules
                .GetLeaveTypeAsync(
                    writeRepository,
                    request.LeaveTypeId,
                    cancellationToken);

        await LeaveRequestBusinessRules
            .EnsureNoOverlapAsync(
                writeRepository,
                request.EmployeeId,
                request.StartDate,
                request.EndDate,
                excludedLeaveRequestId: null,
                cancellationToken: cancellationToken);

        await LeaveRequestBusinessRules
            .EnsureEnoughBalanceAsync(
                writeRepository,
                request.EmployeeId,
                leaveType,
                request.StartDate,
                request.EndDate,
                excludedLeaveRequestId: null,
                cancellationToken: cancellationToken);

        var leaveRequest =
            new LeaveRequest
            {
                EmployeeId = request.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,
                Reason = reason
            };

        leaveRequest.SetDateRange(
            request.StartDate,
            request.EndDate);

        writeRepository.Add(
            leaveRequest);

        await writeRepository.SaveChangesAsync(
            cancellationToken);

        return await readRepository.GetByIdAsync(
                   leaveRequest.Id,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "Leave request was created but could not be loaded.");
    }
}
