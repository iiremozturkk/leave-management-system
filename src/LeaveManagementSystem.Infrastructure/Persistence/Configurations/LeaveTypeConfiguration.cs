using LeaveManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes");

        builder.HasKey(leaveType => leaveType.Id);

        builder.Property(leaveType => leaveType.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(leaveType => leaveType.Name)
            .IsUnique();

        builder.Property(leaveType => leaveType.DefaultAnnualAllowanceDays)
            .IsRequired();

        builder.Property(leaveType => leaveType.IsPaid)
            .IsRequired();

        builder.Property(leaveType => leaveType.CreatedAtUtc)
            .IsRequired();

        builder.Property(leaveType => leaveType.UpdatedAtUtc);
    }
}