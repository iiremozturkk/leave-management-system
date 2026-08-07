using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Application.LeaveRequests.Abstractions;

public interface ILeaveRequestNotificationService
{
    Task NotifyReviewCompletedAsync(
        Guid leaveRequestId,
        Guid employeeId,
        Guid reviewerEmployeeId,
        LeaveRequestStatus status,
        DateTime reviewedAtUtc,
        CancellationToken cancellationToken = default);
}
