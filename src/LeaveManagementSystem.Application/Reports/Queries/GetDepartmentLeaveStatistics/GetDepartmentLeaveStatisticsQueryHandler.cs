using LeaveManagementSystem.Application.Reports.Abstractions;
using LeaveManagementSystem.Application.Reports.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.Reports.Queries.GetDepartmentLeaveStatistics;

public sealed class GetDepartmentLeaveStatisticsQueryHandler(
    IDepartmentLeaveStatisticsReadRepository
        departmentLeaveStatisticsReadRepository)
    : IRequestHandler<
        GetDepartmentLeaveStatisticsQuery,
        IReadOnlyList<DepartmentLeaveStatisticsDto>>
{
    public async Task<IReadOnlyList<DepartmentLeaveStatisticsDto>> Handle(
        GetDepartmentLeaveStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        return await departmentLeaveStatisticsReadRepository.GetAsync(
            cancellationToken);
    }
}
