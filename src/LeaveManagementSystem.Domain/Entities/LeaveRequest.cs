using LeaveManagementSystem.Domain.Common;
using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Domain.Entities;

public sealed class LeaveRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public Guid LeaveTypeId { get; set; }

    public LeaveType LeaveType { get; set; } = null!;

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public int RequestedDays { get; private set; }

    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    public string Reason { get; set; } = string.Empty;

    public string? ManagerComment { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public Guid? ReviewedByEmployeeId { get; set; }

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
}