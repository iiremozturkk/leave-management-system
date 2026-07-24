using System.Linq.Expressions;
using LeaveManagementSystem.Application.Employees.Dtos;
using LeaveManagementSystem.Domain.Entities;

namespace LeaveManagementSystem.Infrastructure.Employees.Persistence;

internal static class EmployeeProjections
{
    internal static Expression<Func<Employee, EmployeeDto>> ToDto { get; } =
        employee => new EmployeeDto(
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.Email,
            employee.Role,
            employee.IsActive,
            employee.DepartmentId,
            employee.Department.Name,
            employee.ManagerId,
            employee.Manager == null
                ? null
                : employee.Manager.FirstName + " " + employee.Manager.LastName,
            employee.CreatedAtUtc,
            employee.UpdatedAtUtc);
}
