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
    private const int MinSupportedYear = 2000;
    private const int MaxSupportedYear = 2100;

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
        var requestedDays = CalculateRequestedDays(request.StartDate, request.EndDate);
        var leaveYear = request.StartDate.Year;

        EnsureSupportedYear(leaveYear);

        await EnsureEmployeeExistsAndIsActiveAsync(request.EmployeeId, cancellationToken);
        await EnsureLeaveTypeExistsAsync(request.LeaveTypeId, cancellationToken);

        await EnsureNoOverlappingLeaveRequestAsync(
            request.EmployeeId,
            request.StartDate,
            request.EndDate,
            null,
            cancellationToken);

        await EnsureEnoughLeaveBalanceAsync(
            request.EmployeeId,
            request.LeaveTypeId,
            leaveYear,
            requestedDays,
            null,
            cancellationToken);

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
        var requestedDays = CalculateRequestedDays(request.StartDate, request.EndDate);
        var leaveYear = request.StartDate.Year;

        EnsureSupportedYear(leaveYear);

        await EnsureLeaveTypeExistsAsync(request.LeaveTypeId, cancellationToken);

        await EnsureNoOverlappingLeaveRequestAsync(
            leaveRequest.EmployeeId,
            request.StartDate,
            request.EndDate,
            leaveRequest.Id,
            cancellationToken);

        await EnsureEnoughLeaveBalanceAsync(
            leaveRequest.EmployeeId,
            request.LeaveTypeId,
            leaveYear,
            requestedDays,
            leaveRequest.Id,
            cancellationToken);

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

    public async Task<LeaveBalanceDto?> GetBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new InvalidOperationException("Employee id cannot be empty.");
        }

        if (leaveTypeId == Guid.Empty)
        {
            throw new InvalidOperationException("Leave type id cannot be empty.");
        }

        EnsureSupportedYear(year);

        var employeeExists = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(
                employee => employee.Id == employeeId && employee.IsActive,
                cancellationToken);

        if (!employeeExists)
        {
            return null;
        }

        return await CalculateBalanceAsync(
            employeeId,
            leaveTypeId,
            year,
            null,
            cancellationToken);
    }

    public async Task<LeaveRequestDto?> ApproveAsync(
        Guid id,
        ReviewLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var leaveRequest = await dbContext.LeaveRequests
            .FirstOrDefaultAsync(leaveRequest => leaveRequest.Id == id, cancellationToken);

        if (leaveRequest is null)
        {
            return null;
        }

        await EnsureReviewerCanReviewAsync(
            leaveRequest,
            request.ReviewerEmployeeId,
            cancellationToken);

        var leaveYear = leaveRequest.StartDate.Year;

        EnsureSupportedYear(leaveYear);

        await EnsureEnoughLeaveBalanceAsync(
            leaveRequest.EmployeeId,
            leaveRequest.LeaveTypeId,
            leaveYear,
            leaveRequest.RequestedDays,
            leaveRequest.Id,
            cancellationToken);

        leaveRequest.Approve(request.ReviewerEmployeeId, request.ManagerComment);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(leaveRequest.Id, cancellationToken);
    }

    public async Task<LeaveRequestDto?> RejectAsync(
        Guid id,
        ReviewLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var leaveRequest = await dbContext.LeaveRequests
            .FirstOrDefaultAsync(leaveRequest => leaveRequest.Id == id, cancellationToken);

        if (leaveRequest is null)
        {
            return null;
        }

        await EnsureReviewerCanReviewAsync(
            leaveRequest,
            request.ReviewerEmployeeId,
            cancellationToken);

        leaveRequest.Reject(request.ReviewerEmployeeId, request.ManagerComment);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(leaveRequest.Id, cancellationToken);
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

    private async Task EnsureNoOverlappingLeaveRequestAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? currentLeaveRequestId,
        CancellationToken cancellationToken)
    {
        var hasOverlap = await dbContext.LeaveRequests
            .AnyAsync(
                leaveRequest =>
                    leaveRequest.EmployeeId == employeeId
                    && leaveRequest.Status != LeaveRequestStatus.Rejected
                    && (currentLeaveRequestId == null || leaveRequest.Id != currentLeaveRequestId.Value)
                    && leaveRequest.StartDate <= endDate
                    && startDate <= leaveRequest.EndDate,
                cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException(
                "Employee already has a leave request in the selected date range.");
        }
    }

    private async Task EnsureEnoughLeaveBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        int requestedDays,
        Guid? excludedLeaveRequestId,
        CancellationToken cancellationToken)
    {
        var balance = await CalculateBalanceAsync(
            employeeId,
            leaveTypeId,
            year,
            excludedLeaveRequestId,
            cancellationToken);

        if (balance is null)
        {
            throw new InvalidOperationException("Leave type does not exist.");
        }

        if (balance.EntitledDays <= 0)
        {
            return;
        }

        if (requestedDays > balance.RemainingDays)
        {
            throw new InvalidOperationException(
                "Requested leave days exceed the remaining leave balance.");
        }
    }

    private async Task<LeaveBalanceDto?> CalculateBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        Guid? excludedLeaveRequestId,
        CancellationToken cancellationToken)
    {
        var leaveType = await dbContext.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(leaveType => leaveType.Id == leaveTypeId, cancellationToken);

        if (leaveType is null)
        {
            return null;
        }

        var yearStart = new DateOnly(year, 1, 1);
        var nextYearStart = yearStart.AddYears(1);

        var usedDays = await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(leaveRequest =>
                leaveRequest.EmployeeId == employeeId
                && leaveRequest.LeaveTypeId == leaveTypeId
                && leaveRequest.Status == LeaveRequestStatus.Approved
                && leaveRequest.StartDate >= yearStart
                && leaveRequest.StartDate < nextYearStart
                && (excludedLeaveRequestId == null || leaveRequest.Id != excludedLeaveRequestId.Value))
            .SumAsync(leaveRequest => leaveRequest.RequestedDays, cancellationToken);

        var entitledDays = leaveType.DefaultAnnualAllowanceDays;
        var remainingDays = entitledDays - usedDays;

        return new LeaveBalanceDto(
            employeeId,
            leaveType.Id,
            leaveType.Name,
            year,
            entitledDays,
            usedDays,
            remainingDays);
    }

    private async Task EnsureReviewerCanReviewAsync(
        LeaveRequest leaveRequest,
        Guid reviewerEmployeeId,
        CancellationToken cancellationToken)
    {
        if (reviewerEmployeeId == Guid.Empty)
        {
            throw new InvalidOperationException("Reviewer employee id cannot be empty.");
        }

        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                employee => employee.Id == leaveRequest.EmployeeId,
                cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            throw new InvalidOperationException("Employee does not exist or is not active.");
        }

        var reviewer = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                employee => employee.Id == reviewerEmployeeId,
                cancellationToken);

        if (reviewer is null || !reviewer.IsActive)
        {
            throw new InvalidOperationException("Reviewer does not exist or is not active.");
        }

        if (reviewer.Role != EmployeeRole.Manager)
        {
            throw new InvalidOperationException("Only managers can review leave requests.");
        }

        if (employee.ManagerId != reviewerEmployeeId)
        {
            throw new InvalidOperationException(
                "Only the employee's direct manager can review this leave request.");
        }
    }

    private static void EnsureSupportedYear(int year)
    {
        if (year < MinSupportedYear || year > MaxSupportedYear)
        {
            throw new InvalidOperationException(
                $"Year must be between {MinSupportedYear} and {MaxSupportedYear}.");
        }
    }

    private static int CalculateRequestedDays(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new InvalidOperationException("End date cannot be earlier than start date.");
        }

        return endDate.DayNumber - startDate.DayNumber + 1;
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