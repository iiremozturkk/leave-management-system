using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.LeaveRequests.Persistence;

public sealed class LeaveRequestReadRepository(
    AppDbContext dbContext)
    : ILeaveRequestReadRepository,
      ILeaveRequestScopedReadRepository,
      ILeaveCalendarReadRepository
{
    public async Task<IReadOnlyList<LeaveRequestDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .OrderByDescending(
                leaveRequest =>
                    leaveRequest.CreatedAtUtc)
            .Select(LeaveRequestProjections.ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(
                leaveRequest =>
                    leaveRequest.Id == id)
            .Select(LeaveRequestProjections.ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>>
        GetAllForEmployeeAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(
                leaveRequest =>
                    leaveRequest.EmployeeId == employeeId)
            .OrderByDescending(
                leaveRequest =>
                    leaveRequest.CreatedAtUtc)
            .Select(LeaveRequestProjections.ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveCalendarItemDto>>
    GetCalendarAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(
                leaveRequest =>
                    leaveRequest.StartDate <= endDate
                    && leaveRequest.EndDate >= startDate)
            .OrderBy(
                leaveRequest =>
                    leaveRequest.StartDate)
            .ThenBy(
                leaveRequest =>
                    leaveRequest.EndDate)
            .ThenBy(
                leaveRequest =>
                    leaveRequest.Employee.LastName)
            .ThenBy(
                leaveRequest =>
                    leaveRequest.Employee.FirstName)
            .Select(
                LeaveRequestProjections.ToCalendarItem)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveCalendarItemDto>>
        GetCalendarForEmployeeAsync(
            Guid employeeId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(
                leaveRequest =>
                    leaveRequest.EmployeeId == employeeId
                    && leaveRequest.StartDate <= endDate
                    && leaveRequest.EndDate >= startDate)
            .OrderBy(
                leaveRequest =>
                    leaveRequest.StartDate)
            .ThenBy(
                leaveRequest =>
                    leaveRequest.EndDate)
            .Select(
                LeaveRequestProjections.ToCalendarItem)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveCalendarItemDto>>
        GetCalendarForManagerAsync(
            Guid managerId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(
                leaveRequest =>
                    leaveRequest.Employee.ManagerId == managerId
                    && leaveRequest.Employee.IsActive
                    && leaveRequest.StartDate <= endDate
                    && leaveRequest.EndDate >= startDate)
            .OrderBy(
                leaveRequest =>
                    leaveRequest.StartDate)
            .ThenBy(
                leaveRequest =>
                    leaveRequest.EndDate)
            .ThenBy(
                leaveRequest =>
                    leaveRequest.Employee.LastName)
            .ThenBy(
                leaveRequest =>
                    leaveRequest.Employee.FirstName)
            .Select(
                LeaveRequestProjections.ToCalendarItem)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<LeaveRequestDto?> GetByIdForEmployeeAsync(
        Guid id,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(
                leaveRequest =>
                    leaveRequest.Id == id
                    && leaveRequest.EmployeeId == employeeId)
            .Select(LeaveRequestProjections.ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>>
       GetAllForManagerAsync(
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(
                leaveRequest =>
                    leaveRequest.Employee.ManagerId == managerId
                    && leaveRequest.Employee.IsActive)
            .OrderByDescending(
                leaveRequest =>
                    leaveRequest.CreatedAtUtc)
            .Select(LeaveRequestProjections.ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveRequestDto?> GetByIdForManagerAsync(
        Guid id,
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(
                leaveRequest =>
                    leaveRequest.Id == id
                    && (
                        leaveRequest.EmployeeId == managerId
                        || (
                            leaveRequest.Employee.ManagerId == managerId
                            && leaveRequest.Employee.IsActive
                        )
                    ))
            .Select(LeaveRequestProjections.ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
