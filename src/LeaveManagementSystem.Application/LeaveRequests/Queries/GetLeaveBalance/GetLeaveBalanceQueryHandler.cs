using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.LeaveRequests.Queries.GetLeaveBalance;

public sealed class GetLeaveBalanceQueryHandler(
    ILeaveBalanceReadRepository leaveBalanceReadRepository)
    : IRequestHandler<
        GetLeaveBalanceQuery,
        LeaveBalanceDto?>
{
    private const int MinSupportedYear = 2000;
    private const int MaxSupportedYear = 2100;

    public Task<LeaveBalanceDto?> Handle(
        GetLeaveBalanceQuery request,
        CancellationToken cancellationToken)
    {
        if (request.EmployeeId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Employee id cannot be empty.");
        }

        if (request.LeaveTypeId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Leave type id cannot be empty.");
        }

        EnsureSupportedYear(
            request.Year);

        return leaveBalanceReadRepository.GetBalanceAsync(
            request.EmployeeId,
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
