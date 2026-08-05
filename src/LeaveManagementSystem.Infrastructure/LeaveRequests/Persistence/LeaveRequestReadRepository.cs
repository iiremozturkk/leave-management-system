using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.LeaveRequests.Persistence;

public sealed class LeaveRequestReadRepository(
    AppDbContext dbContext)
    : ILeaveRequestReadRepository,
      ILeaveRequestSelfServiceReadRepository
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
}
