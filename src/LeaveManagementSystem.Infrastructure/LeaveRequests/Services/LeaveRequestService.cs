using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Services;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.LeaveRequests.Persistence;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.LeaveRequests.Services;

public sealed class LeaveRequestService(AppDbContext dbContext)
    : ILeaveRequestService
{
    public async Task<LeaveRequestDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(leaveRequest =>
                leaveRequest.Id == id)
            .Select(LeaveRequestProjections.ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LeaveRequestDto?> RejectAsync(
        Guid id,
        ReviewLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var leaveRequest =
            await dbContext.LeaveRequests
                .FirstOrDefaultAsync(
                    leaveRequest =>
                        leaveRequest.Id == id,
                    cancellationToken);

        if (leaveRequest is null)
        {
            return null;
        }

        await EnsureReviewerCanReviewAsync(
            leaveRequest,
            request.ReviewerEmployeeId,
            cancellationToken);

        leaveRequest.Reject(
            request.ReviewerEmployeeId,
            request.ManagerComment);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return await GetByIdAsync(
            leaveRequest.Id,
            cancellationToken);
    }

    private async Task EnsureReviewerCanReviewAsync(
        LeaveRequest leaveRequest,
        Guid reviewerEmployeeId,
        CancellationToken cancellationToken)
    {
        if (reviewerEmployeeId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Reviewer employee id cannot be empty.");
        }

        var employee =
            await dbContext.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    employee =>
                        employee.Id ==
                        leaveRequest.EmployeeId,
                    cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new InvalidOperationException(
                "Employee does not exist or is not active.");
        }

        var reviewer =
            await dbContext.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    employee =>
                        employee.Id ==
                        reviewerEmployeeId,
                    cancellationToken);

        if (reviewer is null || !reviewer.IsActive)
        {
            throw new InvalidOperationException(
                "Reviewer does not exist or is not active.");
        }

        if (reviewer.Role != EmployeeRole.Manager)
        {
            throw new ForbiddenOperationException(
                "Only managers can review leave requests.");
        }

        if (employee.ManagerId != reviewerEmployeeId)
        {
            throw new ForbiddenOperationException(
                "Only the employee's direct manager can review this leave request.");
        }
    }
}
