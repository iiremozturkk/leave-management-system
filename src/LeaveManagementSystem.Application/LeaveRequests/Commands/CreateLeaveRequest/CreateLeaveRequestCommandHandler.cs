using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Rules;
using LeaveManagementSystem.Domain.Entities;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Commands.CreateLeaveRequest;

public sealed class CreateLeaveRequestCommandHandler(
    ILeaveRequestWriteRepository writeRepository,
    ILeaveRequestReadRepository readRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IRequestHandler<CreateLeaveRequestCommand, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(
        CreateLeaveRequestCommand request,
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

        var employeeId =
            currentUserAccess.EmployeeId;

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
                employeeId,
                request.StartDate,
                request.EndDate,
                excludedLeaveRequestId: null,
                cancellationToken: cancellationToken);

        await LeaveRequestBusinessRules
            .EnsureEnoughBalanceAsync(
                writeRepository,
                employeeId,
                leaveType,
                request.StartDate,
                request.EndDate,
                excludedLeaveRequestId: null,
                cancellationToken: cancellationToken);

        var leaveRequest =
            new LeaveRequest
            {
                EmployeeId = employeeId,
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
