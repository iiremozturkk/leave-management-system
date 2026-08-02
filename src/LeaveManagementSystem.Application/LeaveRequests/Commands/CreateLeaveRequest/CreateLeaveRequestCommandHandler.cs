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
        ArgumentNullException.ThrowIfNull(request);

        var reason =
            LeaveRequestRules.NormalizeReason(
                request.Reason);

        LeaveRequestRules.EnsureSupportedDateRange(
            request.StartDate,
            request.EndDate);

        await EnsureEmployeeExistsAndIsActiveAsync(
            request.EmployeeId,
            cancellationToken);

        var leaveType =
            await GetLeaveTypeAsync(
                request.LeaveTypeId,
                cancellationToken);

        await EnsureNoOverlappingLeaveRequestAsync(
            request.EmployeeId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        await EnsureEnoughLeaveBalanceAsync(
            request.EmployeeId,
            leaveType,
            request.StartDate,
            request.EndDate,
            cancellationToken);

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

    private async Task EnsureEmployeeExistsAndIsActiveAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        if (employeeId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Employee id cannot be empty.");
        }

        var employeeExists =
            await writeRepository.ActiveEmployeeExistsAsync(
                employeeId,
                cancellationToken);

        if (!employeeExists)
        {
            throw new InvalidOperationException(
                "Employee does not exist or is not active.");
        }
    }

    private async Task<LeaveType> GetLeaveTypeAsync(
        Guid leaveTypeId,
        CancellationToken cancellationToken)
    {
        if (leaveTypeId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Leave type id cannot be empty.");
        }

        return await writeRepository.GetLeaveTypeAsync(
                   leaveTypeId,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "Leave type does not exist.");
    }

    private async Task EnsureNoOverlappingLeaveRequestAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var hasOverlap =
            await writeRepository.HasOverlappingLeaveRequestAsync(
                employeeId,
                startDate,
                endDate,
                excludedLeaveRequestId: null,
                cancellationToken: cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException(
                "Employee already has a leave request in the selected date range.");
        }
    }

    private async Task EnsureEnoughLeaveBalanceAsync(
        Guid employeeId,
        LeaveType leaveType,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var requestedDaysByYear =
            LeaveRequestRules.GetRequestedDaysByYear(
                startDate,
                endDate);

        foreach (var requestedDaysForYear in requestedDaysByYear)
        {
            var usedDays =
                await writeRepository.GetApprovedUsedDaysForYearAsync(
                    employeeId,
                    leaveType.Id,
                    requestedDaysForYear.Year,
                    excludedLeaveRequestId: null,
                    cancellationToken: cancellationToken);

            var entitledDays =
                LeaveRequestRules.CalculateEntitledDays(
                    leaveType.DefaultAnnualAllowanceDays,
                    requestedDaysForYear.Year);

            if (entitledDays <= 0)
            {
                continue;
            }

            var remainingDays =
                entitledDays - usedDays;

            if (requestedDaysForYear.Days > remainingDays)
            {
                throw new InvalidOperationException(
                    "Requested leave days exceed the remaining leave balance.");
            }
        }
    }
}
