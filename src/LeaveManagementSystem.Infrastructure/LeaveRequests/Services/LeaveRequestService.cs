using System.Linq.Expressions;
using LeaveManagementSystem.Application.LeaveRequests.Dtos;
using LeaveManagementSystem.Application.LeaveRequests.Services;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagementSystem.Infrastructure.LeaveRequests.Services;

public sealed class LeaveRequestService(AppDbContext dbContext) : ILeaveRequestService
{
    private const int ReasonMaxLength = 500;

    public async Task<IReadOnlyList<LeaveRequestDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .OrderByDescending(leaveRequest => leaveRequest.CreatedAtUtc)
            .Select(LeaveRequestProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(leaveRequest => leaveRequest.Id == id)
            .Select(LeaveRequestProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LeaveRequestDto> CreateAsync(
        CreateLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reason = NormalizeRequiredText(request.Reason, "Reason", ReasonMaxLength);

        await EnsureEmployeeExistsAndIsActiveAsync(request.EmployeeId, cancellationToken);
        await EnsureLeaveTypeExistsAsync(request.LeaveTypeId, cancellationToken);

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = request.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            Reason = reason
        };

        leaveRequest.SetDateRange(request.StartDate, request.EndDate);

        dbContext.LeaveRequests.Add(leaveRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(leaveRequest.Id, cancellationToken)
            ?? throw new InvalidOperationException("Leave request was created but could not be loaded.");
    }

    public async Task<LeaveRequestDto?> UpdateAsync(
        Guid id,
        UpdateLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var leaveRequest = await dbContext.LeaveRequests
            .FirstOrDefaultAsync(leaveRequest => leaveRequest.Id == id, cancellationToken);

        if (leaveRequest is null)
        {
            return null;
        }

        EnsureLeaveRequestCanBeModified(leaveRequest);

        var reason = NormalizeRequiredText(request.Reason, "Reason", ReasonMaxLength);

        await EnsureLeaveTypeExistsAsync(request.LeaveTypeId, cancellationToken);

        leaveRequest.LeaveTypeId = request.LeaveTypeId;
        leaveRequest.Reason = reason;
        leaveRequest.SetDateRange(request.StartDate, request.EndDate);
        leaveRequest.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(leaveRequest.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await dbContext.LeaveRequests
            .FirstOrDefaultAsync(leaveRequest => leaveRequest.Id == id, cancellationToken);

        if (leaveRequest is null)
        {
            return false;
        }

        EnsureLeaveRequestCanBeModified(leaveRequest);

        dbContext.LeaveRequests.Remove(leaveRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task EnsureEmployeeExistsAndIsActiveAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        if (employeeId == Guid.Empty)
        {
            throw new InvalidOperationException("Employee id cannot be empty.");
        }

        var exists = await dbContext.Employees
            .AnyAsync(
                employee => employee.Id == employeeId && employee.IsActive,
                cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Employee does not exist or is not active.");
        }
    }

    private async Task EnsureLeaveTypeExistsAsync(
        Guid leaveTypeId,
        CancellationToken cancellationToken)
    {
        if (leaveTypeId == Guid.Empty)
        {
            throw new InvalidOperationException("Leave type id cannot be empty.");
        }

        var exists = await dbContext.LeaveTypes
            .AnyAsync(leaveType => leaveType.Id == leaveTypeId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Leave type does not exist.");
        }
    }

    private static void EnsureLeaveRequestCanBeModified(LeaveRequest leaveRequest)
    {
        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending leave requests can be modified.");
        }
    }

    private static string NormalizeRequiredText(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} cannot be empty.");
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new InvalidOperationException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalizedValue;
    }

    private static readonly Expression<Func<LeaveRequest, LeaveRequestDto>> LeaveRequestProjection = leaveRequest =>
        new LeaveRequestDto(
            leaveRequest.Id,
            leaveRequest.EmployeeId,
            leaveRequest.Employee.FirstName + " " + leaveRequest.Employee.LastName,
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
                : leaveRequest.ReviewedByEmployee.FirstName + " " + leaveRequest.ReviewedByEmployee.LastName,
            leaveRequest.CreatedAtUtc,
            leaveRequest.UpdatedAtUtc);
}
