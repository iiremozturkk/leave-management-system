using System.Linq.Expressions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Domain.Entities;

namespace LeaveManagementSystem.Infrastructure.LeaveRequests.Persistence;

internal static class LeaveRequestProjections
{
    internal static Expression<Func<LeaveRequest, LeaveRequestDto>> ToDto
    { get; } =
        leaveRequest => new LeaveRequestDto(
            leaveRequest.Id,
            leaveRequest.EmployeeId,
            leaveRequest.Employee.FirstName
                + " "
                + leaveRequest.Employee.LastName,
            leaveRequest.LeaveTypeId,
            leaveRequest.LeaveType.Name,
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.RequestedDays,
            leaveRequest.Status,
            leaveRequest.Reason,
            leaveRequest.ManagerComment,
            leaveRequest.ReviewedAtUtc,
            leaveRequest.ReviewedByEmployeeId,
            leaveRequest.ReviewedByEmployee == null
                ? null
                : leaveRequest.ReviewedByEmployee.FirstName
                    + " "
                    + leaveRequest.ReviewedByEmployee.LastName,
            leaveRequest.CreatedAtUtc,
            leaveRequest.UpdatedAtUtc);

    internal static Expression<Func<LeaveRequest, LeaveCalendarItemDto>>
    ToCalendarItem
    {
        get;
    } =
    leaveRequest =>
        new LeaveCalendarItemDto(
            leaveRequest.Id,
            leaveRequest.EmployeeId,
            leaveRequest.Employee.FirstName
                + " "
                + leaveRequest.Employee.LastName,
            leaveRequest.LeaveTypeId,
            leaveRequest.LeaveType.Name,
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.RequestedDays,
            leaveRequest.Status);
}
