using LeaveManagementSystem.Application.LeaveRequests.Dtos;

namespace LeaveManagementSystem.Application.LeaveRequests.Abstractions;

public interface ILeaveRequestScopedReadRepository
{
    Task<IReadOnlyList<LeaveRequestDto>> GetAllForEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> GetByIdForEmployeeAsync(
        Guid id,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRequestDto>> GetAllForManagerAsync(
        Guid managerId,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> GetByIdForManagerAsync(
        Guid id,
        Guid managerId,
        CancellationToken cancellationToken = default);
}
