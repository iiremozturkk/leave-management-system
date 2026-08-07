using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LeaveManagementSystem.Infrastructure.LeaveRequests.Notifications;

public sealed class LeaveRequestNotificationService(
    ILogger<LeaveRequestNotificationService> logger)
    : ILeaveRequestNotificationService
{
    private const string NotificationType =
        "LeaveRequestReviewCompleted";

    public Task NotifyReviewCompletedAsync(
        Guid leaveRequestId,
        Guid employeeId,
        Guid reviewerEmployeeId,
        LeaveRequestStatus status,
        DateTime reviewedAtUtc,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Notification simulated. NotificationType: {NotificationType}, LeaveRequestId: {LeaveRequestId}, EmployeeId: {EmployeeId}, ReviewerEmployeeId: {ReviewerEmployeeId}, Status: {Status}, ReviewedAtUtc: {ReviewedAtUtc}",
            NotificationType,
            leaveRequestId,
            employeeId,
            reviewerEmployeeId,
            status,
            reviewedAtUtc);

        return Task.CompletedTask;
    }
}
