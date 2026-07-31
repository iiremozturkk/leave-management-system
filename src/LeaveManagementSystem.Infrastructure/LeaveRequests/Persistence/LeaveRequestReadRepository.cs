using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.LeaveRequests.Persistence;

public sealed class LeaveRequestReadRepository(
    AppDbContext dbContext)
    : ILeaveRequestReadRepository
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
}
