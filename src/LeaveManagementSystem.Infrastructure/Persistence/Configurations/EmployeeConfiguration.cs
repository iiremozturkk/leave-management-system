using LeaveManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(employee => employee.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(employee => employee.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(employee => employee.Email)
            .IsUnique();

        builder.Property(employee => employee.Role)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(employee => employee.IsActive)
            .IsRequired();

        builder.Property(employee => employee.CreatedAtUtc)
            .IsRequired();

        builder.Property(employee => employee.UpdatedAtUtc);

        builder.HasOne(employee => employee.Department)
            .WithMany(department => department.Employees)
            .HasForeignKey(employee => employee.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(employee => employee.Manager)
            .WithMany(manager => manager.DirectReports)
            .HasForeignKey(employee => employee.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}