using System.Text.Json;
using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Domain.Common;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LeaveManagementSystem.Infrastructure.Persistence.Auditing;

public sealed class AuditSaveChangesInterceptor(
    ICurrentUser? currentUser,
    TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);

        return base.SavingChanges(
            eventData,
            result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditLogs(eventData.Context);

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    private void AddAuditLogs(
        DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        dbContext.ChangeTracker.DetectChanges();

        var auditedEntries =
            dbContext.ChangeTracker
                .Entries()
                .Where(entry =>
                    (entry.Entity is Employee or LeaveRequest)
                    && entry.State is
                        EntityState.Added
                        or EntityState.Modified
                        or EntityState.Deleted)
                .ToArray();

        if (auditedEntries.Length == 0)
        {
            return;
        }

        var actorEmployeeId =
            GetActorEmployeeId();

        var occurredAtUtc =
            timeProvider.GetUtcNow().UtcDateTime;

        var auditLogs =
            auditedEntries
                .Select(entry =>
                    CreateAuditLog(
                        entry,
                        actorEmployeeId,
                        occurredAtUtc))
                .ToArray();

        dbContext.Set<AuditLog>()
            .AddRange(auditLogs);
    }

    private static AuditLog CreateAuditLog(
        EntityEntry entry,
        Guid? actorEmployeeId,
        DateTime occurredAtUtc)
    {
        var entity =
            (BaseEntity)entry.Entity;

        return new AuditLog
        {
            EntityName =
                entry.Metadata.ClrType.Name,

            EntityId =
                entity.Id,

            Action =
                GetAuditAction(entry),

            ChangedPropertiesJson =
                JsonSerializer.Serialize(
                    GetChangedPropertyNames(entry)),

            ActorEmployeeId =
                actorEmployeeId,

            OccurredAtUtc =
                occurredAtUtc
        };
    }

    private static AuditAction GetAuditAction(
        EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            return AuditAction.Created;
        }

        if (entry.State == EntityState.Deleted)
        {
            return AuditAction.Deleted;
        }

        if (entry.Entity is LeaveRequest)
        {
            var statusProperty =
                entry.Property(
                    nameof(LeaveRequest.Status));

            if (statusProperty.IsModified
                && statusProperty.OriginalValue
                    is LeaveRequestStatus originalStatus
                && statusProperty.CurrentValue
                    is LeaveRequestStatus currentStatus
                && originalStatus == LeaveRequestStatus.Pending)
            {
                if (currentStatus ==
                    LeaveRequestStatus.Approved)
                {
                    return AuditAction.Approved;
                }

                if (currentStatus ==
                    LeaveRequestStatus.Rejected)
                {
                    return AuditAction.Rejected;
                }
            }
        }

        return AuditAction.Updated;
    }

    private static string[] GetChangedPropertyNames(
        EntityEntry entry)
    {
        IEnumerable<PropertyEntry> properties =
            entry.State == EntityState.Modified
                ? entry.Properties.Where(
                    property =>
                        property.IsModified
                        && property.Metadata.Name
                            != nameof(BaseEntity.UpdatedAtUtc))
                : entry.Properties;

        return properties
            .Select(property =>
                property.Metadata.Name)
            .OrderBy(
                propertyName =>
                    propertyName,
                StringComparer.Ordinal)
            .ToArray();
    }

    private Guid? GetActorEmployeeId()
    {
        if (currentUser?.IsAuthenticated != true)
        {
            return null;
        }

        var employeeId =
            currentUser.EmployeeId;

        return employeeId.HasValue
            && employeeId.Value != Guid.Empty
                ? employeeId.Value
                : null;
    }
}
