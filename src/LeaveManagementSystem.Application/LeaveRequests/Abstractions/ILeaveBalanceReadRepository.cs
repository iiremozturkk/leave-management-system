using LeaveManagementSystem.Application.LeaveRequests.Dtos;

namespace LeaveManagementSystem.Application.LeaveRequests.Abstractions;

public interface ILeaveBalanceReadRepository
{
    Task<LeaveBalanceDto?> GetBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        Guid? excludedLeaveRequestId = null,
        CancellationToken cancellationToken = default);
}
