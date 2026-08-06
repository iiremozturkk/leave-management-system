using LeaveManagementSystem.Application.Reports.Dtos;

namespace LeaveManagementSystem.Application.Reports.Abstractions;

public interface IDepartmentLeaveStatisticsReadRepository
{
    Task<IReadOnlyList<DepartmentLeaveStatisticsDto>> GetAsync(
        CancellationToken cancellationToken = default);
}
