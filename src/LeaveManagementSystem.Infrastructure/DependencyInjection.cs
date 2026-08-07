using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Employees.Abstractions;
using LeaveManagementSystem.Application.LeaveRequests.Abstractions;
using LeaveManagementSystem.Application.Reports.Abstractions;
using LeaveManagementSystem.Infrastructure.DemoData;
using LeaveManagementSystem.Infrastructure.Authentication.Jwt;
using LeaveManagementSystem.Infrastructure.Authentication.Persistence;
using LeaveManagementSystem.Infrastructure.Authentication.Security;
using LeaveManagementSystem.Infrastructure.Employees.Persistence;
using LeaveManagementSystem.Infrastructure.LeaveRequests.Persistence;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.Infrastructure.Reports.Persistence;
using LeaveManagementSystem.Infrastructure.LeaveRequests.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LeaveManagementSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection connection string is not configured.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddOptions<DemoDataOptions>()
            .Bind(
                configuration.GetSection(
                    DemoDataOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<DemoDataOptions>,
            DemoDataOptionsValidator>();

        services.AddScoped<DemoDataSeeder>();

        services.AddOptions<JwtOptions>()
            .Bind(
                configuration.GetSection(
                    JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<JwtOptions>,
            JwtOptionsValidator>();

        services.AddSingleton<TimeProvider>(
            TimeProvider.System);

        services.AddSingleton<
            IJwtTokenGenerator,
            JwtTokenGenerator>();

        services.AddScoped<
            IPasswordHashService,
            PasswordHashService>();

        services.AddScoped<
            ICurrentUserAccessReadRepository,
            CurrentUserAccessReadRepository>();

        services.AddScoped<
            IUserAccountReadRepository,
            UserAccountReadRepository>();

        services.AddScoped<
            IUserAccountWriteRepository,
            UserAccountWriteRepository>();

        services.AddScoped<
            IEmployeeReadRepository,
            EmployeeReadRepository>();

        services.AddScoped<
            IEmployeeWriteRepository,
            EmployeeWriteRepository>();

        services.AddScoped<
            IEmployeeAdministrationTransactionManager,
            EmployeeAdministrationTransactionManager>();

        services.AddScoped<
            ILeaveRequestReadRepository,
            LeaveRequestReadRepository>();

        services.AddScoped<
            ILeaveCalendarReadRepository,
            LeaveRequestReadRepository>();

        services.AddScoped<
            ILeaveRequestScopedReadRepository,
            LeaveRequestReadRepository>();

        services.AddScoped<
            ILeaveBalanceReadRepository,
            LeaveBalanceReadRepository>();

        services.AddScoped<
            ILeaveRequestWriteRepository,
            LeaveRequestWriteRepository>();

        services.AddScoped<
            ILeaveRequestNotificationService,
            LeaveRequestNotificationService>();

        services.AddScoped<
            IDepartmentLeaveStatisticsReadRepository,
            DepartmentLeaveStatisticsReadRepository>();

        return services;
    }
}
