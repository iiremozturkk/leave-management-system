using LeaveManagementSystem.Application.Reports.Dtos;
using MediatR;

namespace LeaveManagementSystem.Application.Reports.Queries.GetDepartmentLeaveStatistics;

public sealed record GetDepartmentLeaveStatisticsQuery
    : IRequest<IReadOnlyList<DepartmentLeaveStatisticsDto>>;
