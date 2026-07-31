using LeaveManagementSystem.Application.LeaveRequests.Dtos;

namespace LeaveManagementSystem.Application.LeaveRequests.Abstractions;

public interface ILeaveRequestReadRepository
{
    Task<IReadOnlyList<LeaveRequestDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<LeaveRequestDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
