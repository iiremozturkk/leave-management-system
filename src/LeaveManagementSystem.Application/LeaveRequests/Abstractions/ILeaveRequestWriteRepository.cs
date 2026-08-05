using LeaveManagementSystem.Domain.Entities;

namespace LeaveManagementSystem.Application.LeaveRequests.Abstractions;

public interface ILeaveRequestWriteRepository
{
    Task<LeaveRequest?> GetForModificationAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<LeaveRequest?> GetForModificationForEmployeeAsync(
        Guid id,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveEmployeeExistsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<LeaveType?> GetLeaveTypeAsync(
        Guid leaveTypeId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingLeaveRequestAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludedLeaveRequestId,
        CancellationToken cancellationToken = default);

    Task<int> GetApprovedUsedDaysForYearAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        Guid? excludedLeaveRequestId,
        CancellationToken cancellationToken = default);

    void Add(
        LeaveRequest leaveRequest);

    void Remove(
        LeaveRequest leaveRequest);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
