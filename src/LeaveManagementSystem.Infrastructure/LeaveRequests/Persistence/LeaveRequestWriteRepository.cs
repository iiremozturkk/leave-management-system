using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.LeaveRequests.Persistence;

public sealed class LeaveRequestWriteRepository(
    AppDbContext dbContext)
    : ILeaveRequestWriteRepository
{
    public Task<LeaveRequest?> GetForModificationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.LeaveRequests
            .FirstOrDefaultAsync(
                leaveRequest =>
                    leaveRequest.Id == id,
                cancellationToken);
    }

    public Task<LeaveRequest?> GetForModificationForEmployeeAsync(
        Guid id,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.LeaveRequests
            .FirstOrDefaultAsync(
                leaveRequest =>
                    leaveRequest.Id == id
                    && leaveRequest.EmployeeId == employeeId,
                cancellationToken);
    }

    public Task<bool> ActiveEmployeeExistsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Employees
            .AsNoTracking()
            .AnyAsync(
                employee =>
                    employee.Id == employeeId
                    && employee.IsActive,
                cancellationToken);
    }

    public Task<LeaveType?> GetLeaveTypeAsync(
        Guid leaveTypeId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                leaveType =>
                    leaveType.Id == leaveTypeId,
                cancellationToken);
    }

    public Task<bool> HasOverlappingLeaveRequestAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludedLeaveRequestId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.LeaveRequests
            .AsNoTracking()
            .AnyAsync(
                leaveRequest =>
                    leaveRequest.EmployeeId == employeeId
                    && leaveRequest.Status !=
                        LeaveRequestStatus.Rejected
                    && (!excludedLeaveRequestId.HasValue
                        || leaveRequest.Id !=
                            excludedLeaveRequestId.Value)
                    && leaveRequest.StartDate <= endDate
                    && startDate <= leaveRequest.EndDate,
                cancellationToken);
    }

    public Task<int> GetApprovedUsedDaysForYearAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        Guid? excludedLeaveRequestId,
        CancellationToken cancellationToken = default)
    {
        return LeaveBalanceQueries
            .GetApprovedUsedDaysForYearAsync(
                dbContext,
                employeeId,
                leaveTypeId,
                year,
                excludedLeaveRequestId,
                cancellationToken);
    }

    public void Add(
        LeaveRequest leaveRequest)
    {
        ArgumentNullException.ThrowIfNull(
            leaveRequest);

        dbContext.LeaveRequests.Add(
            leaveRequest);
    }

    public void Remove(
        LeaveRequest leaveRequest)
    {
        ArgumentNullException.ThrowIfNull(
            leaveRequest);

        dbContext.LeaveRequests.Remove(
            leaveRequest);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
