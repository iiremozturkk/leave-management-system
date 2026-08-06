using LeaveManagementSystem.Application.LeaveRequests.Dtos;

namespace LeaveManagementSystem.Application.LeaveRequests.Abstractions;

public interface ILeaveCalendarReadRepository
{
    Task<IReadOnlyList<LeaveCalendarItemDto>> GetCalendarAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveCalendarItemDto>>
        GetCalendarForEmployeeAsync(
            Guid employeeId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveCalendarItemDto>>
        GetCalendarForManagerAsync(
            Guid managerId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default);
}
