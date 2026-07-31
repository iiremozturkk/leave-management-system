using LeaveManagementSystem.Application.LeaveRequests.Dtos;

namespace LeaveManagementSystem.Application.LeaveRequests.Services;

public interface ILeaveRequestService
{
    Task<LeaveRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LeaveRequestDto> CreateAsync(
        CreateLeaveRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> UpdateAsync(
        Guid id,
        UpdateLeaveRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LeaveBalanceDto?> GetBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> ApproveAsync(
        Guid id,
        ReviewLeaveRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> RejectAsync(
        Guid id,
        ReviewLeaveRequestRequest request,
        CancellationToken cancellationToken = default);
}