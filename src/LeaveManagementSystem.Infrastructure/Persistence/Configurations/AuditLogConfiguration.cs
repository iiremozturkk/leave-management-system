using LeaveManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration
    : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(
        EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.EntityName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(auditLog => auditLog.EntityId)
            .IsRequired();

        builder.Property(auditLog => auditLog.Action)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(auditLog => auditLog.ChangedPropertiesJson)
            .IsRequired();

        builder.Property(auditLog => auditLog.ActorEmployeeId);

        builder.Property(auditLog => auditLog.OccurredAtUtc)
            .IsRequired();

        builder.HasIndex(auditLog => auditLog.EntityName);

        builder.HasIndex(auditLog => auditLog.EntityId);

        builder.HasIndex(auditLog => auditLog.ActorEmployeeId);

        builder.HasIndex(auditLog => auditLog.OccurredAtUtc);
    }
}
