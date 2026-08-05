using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveBalance;

public sealed class GetLeaveBalanceQueryHandler(
    ILeaveBalanceReadRepository leaveBalanceReadRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IRequestHandler<
        GetLeaveBalanceQuery,
        LeaveBalanceDto?>
{
    private const int MinSupportedYear = 2000;
    private const int MaxSupportedYear = 2100;

    public async Task<LeaveBalanceDto?> Handle(
        GetLeaveBalanceQuery request,
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

        if (request.LeaveTypeId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Leave type id cannot be empty.");
        }

        EnsureSupportedYear(
            request.Year);

        return await leaveBalanceReadRepository.GetBalanceAsync(
            currentUserAccess.EmployeeId,
            request.LeaveTypeId,
            request.Year,
            excludedLeaveRequestId: null,
            cancellationToken: cancellationToken);
    }

    private static void EnsureSupportedYear(
        int year)
    {
        if (year < MinSupportedYear
            || year > MaxSupportedYear)
        {
            throw new InvalidOperationException(
                $"Year must be between {MinSupportedYear} and {MaxSupportedYear}.");
        }
    }
}
