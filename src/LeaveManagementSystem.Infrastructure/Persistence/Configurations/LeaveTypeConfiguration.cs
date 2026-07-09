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

        var seedCreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new LeaveType
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Name = "Annual Leave",
                DefaultAnnualAllowanceDays = 20,
                IsPaid = true,
                CreatedAtUtc = seedCreatedAtUtc,
                UpdatedAtUtc = null
            },
            new LeaveType
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Name = "Sick Leave",
                DefaultAnnualAllowanceDays = 0,
                IsPaid = true,
                CreatedAtUtc = seedCreatedAtUtc,
                UpdatedAtUtc = null
            },
            new LeaveType
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Name = "Unpaid Leave",
                DefaultAnnualAllowanceDays = 0,
                IsPaid = false,
                CreatedAtUtc = seedCreatedAtUtc,
                UpdatedAtUtc = null
            });
    }
}