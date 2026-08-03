using LeaveManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration
    : IEntityTypeConfiguration<UserAccount>
{
    private const int PasswordHashMaxLength = 512;

    public void Configure(
        EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");

        builder.HasKey(userAccount => userAccount.Id);

        builder.Property(userAccount => userAccount.EmployeeId)
            .IsRequired();

        builder.HasIndex(userAccount => userAccount.EmployeeId)
            .IsUnique();

        builder.Property(userAccount => userAccount.PasswordHash)
            .IsRequired()
            .HasMaxLength(PasswordHashMaxLength);

        builder.Property(userAccount => userAccount.IsActive)
            .IsRequired();

        builder.Property(userAccount => userAccount.CreatedAtUtc)
            .IsRequired();

        builder.Property(userAccount => userAccount.UpdatedAtUtc);

        builder.HasOne(userAccount => userAccount.Employee)
            .WithOne(employee => employee.UserAccount)
            .HasForeignKey<UserAccount>(
                userAccount => userAccount.EmployeeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
