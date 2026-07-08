using LeaveManagementSystem.Domain.Common;

namespace LeaveManagementSystem.Domain.Entities;

public sealed class LeaveType : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public int DefaultAnnualAllowanceDays { get; set; }

    public bool IsPaid { get; set; } = true;

    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}