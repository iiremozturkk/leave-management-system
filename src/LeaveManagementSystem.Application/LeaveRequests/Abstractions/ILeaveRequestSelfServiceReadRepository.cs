using LeaveManagementSystem.Application.LeaveRequests.Dtos;

namespace LeaveManagementSystem.Application.LeaveRequests.Abstractions;

public interface ILeaveRequestSelfServiceReadRepository
{
    Task<IReadOnlyList<LeaveRequestDto>> GetAllForEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> GetByIdForEmployeeAsync(
        Guid id,
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
