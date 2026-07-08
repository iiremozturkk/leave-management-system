using LeaveManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");

        builder.HasKey(leaveRequest => leaveRequest.Id);

        builder.Property(leaveRequest => leaveRequest.StartDate)
            .IsRequired();

        builder.Property(leaveRequest => leaveRequest.EndDate)
            .IsRequired();

        builder.Property(leaveRequest => leaveRequest.RequestedDays)
            .IsRequired();

        builder.Property(leaveRequest => leaveRequest.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(leaveRequest => leaveRequest.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(leaveRequest => leaveRequest.ManagerComment)
            .HasMaxLength(500);

        builder.Property(leaveRequest => leaveRequest.ReviewedAtUtc);

        builder.Property(leaveRequest => leaveRequest.CreatedAtUtc)
            .IsRequired();

        builder.Property(leaveRequest => leaveRequest.UpdatedAtUtc);

        builder.HasIndex(leaveRequest => leaveRequest.EmployeeId);

        builder.HasIndex(leaveRequest => leaveRequest.LeaveTypeId);

        builder.HasIndex(leaveRequest => leaveRequest.Status);

        builder.HasIndex(leaveRequest => new
        {
            leaveRequest.EmployeeId,
            leaveRequest.StartDate,
            leaveRequest.EndDate
        });

        builder.HasOne(leaveRequest => leaveRequest.Employee)
            .WithMany(employee => employee.LeaveRequests)
            .HasForeignKey(leaveRequest => leaveRequest.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(leaveRequest => leaveRequest.LeaveType)
            .WithMany(leaveType => leaveType.LeaveRequests)
            .HasForeignKey(leaveRequest => leaveRequest.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(leaveRequest => leaveRequest.ReviewedByEmployee)
            .WithMany()
            .HasForeignKey(leaveRequest => leaveRequest.ReviewedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}