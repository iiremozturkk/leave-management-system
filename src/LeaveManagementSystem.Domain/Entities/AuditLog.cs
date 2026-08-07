using LeaveManagementSystem.Domain.Enums;

namespace LeaveManagementSystem.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public AuditAction Action { get; set; }

    public string ChangedPropertiesJson { get; set; } = string.Empty;

    public Guid? ActorEmployeeId { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
