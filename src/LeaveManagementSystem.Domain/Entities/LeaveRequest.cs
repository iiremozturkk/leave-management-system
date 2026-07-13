using LeaveManagementSystem.Domain.Common;
using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Domain.Entities;

public sealed class LeaveRequest : BaseEntity
{
    private const int ManagerCommentMaxLength = 500;

    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public Guid LeaveTypeId { get; set; }

    public LeaveType LeaveType { get; set; } = null!;

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public int RequestedDays { get; private set; }

    public LeaveRequestStatus Status { get; private set; } = LeaveRequestStatus.Pending;

    public string Reason { get; set; } = string.Empty;

    public string? ManagerComment { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public Guid? ReviewedByEmployeeId { get; private set; }

    public Employee? ReviewedByEmployee { get; set; }

    public void SetDateRange(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("End date cannot be earlier than start date.");
        }

        StartDate = startDate;
        EndDate = endDate;
        RequestedDays = endDate.DayNumber - startDate.DayNumber + 1;
    }

    public void Approve(Guid reviewerEmployeeId, string? managerComment)
    {
        EnsurePending();

        if (reviewerEmployeeId == Guid.Empty)
        {
            throw new InvalidOperationException("Reviewer employee id cannot be empty.");
        }

        var reviewedAtUtc = DateTime.UtcNow;

        Status = LeaveRequestStatus.Approved;
        ManagerComment = NormalizeOptionalComment(managerComment);
        ReviewedAtUtc = reviewedAtUtc;
        ReviewedByEmployeeId = reviewerEmployeeId;
        UpdatedAtUtc = reviewedAtUtc;
    }

    public void Reject(Guid reviewerEmployeeId, string? managerComment)
    {
        EnsurePending();

        if (reviewerEmployeeId == Guid.Empty)
        {
            throw new InvalidOperationException("Reviewer employee id cannot be empty.");
        }

        var reviewedAtUtc = DateTime.UtcNow;

        Status = LeaveRequestStatus.Rejected;
        ManagerComment = NormalizeOptionalComment(managerComment);
        ReviewedAtUtc = reviewedAtUtc;
        ReviewedByEmployeeId = reviewerEmployeeId;
        UpdatedAtUtc = reviewedAtUtc;
    }

    public bool OverlapsWith(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("End date cannot be earlier than start date.");
        }

        return StartDate <= endDate && startDate <= EndDate;
    }

    private void EnsurePending()
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending leave requests can be reviewed.");
        }
    }

    private static string? NormalizeOptionalComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var normalizedComment = comment.Trim();

        if (normalizedComment.Length > ManagerCommentMaxLength)
        {
            throw new InvalidOperationException(
                $"Manager comment cannot exceed {ManagerCommentMaxLength} characters.");
        }

        return normalizedComment;
    }
}