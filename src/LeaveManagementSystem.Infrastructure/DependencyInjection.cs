using LeaveManagementSystem.Application.Employees.Services;
using LeaveManagementSystem.Application.LeaveRequests.Services;
using LeaveManagementSystem.Infrastructure.Employees.Services;
using LeaveManagementSystem.Infrastructure.LeaveRequests.Services;
using LeaveManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagementSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection connection string is not configured.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();

        return services;
    }
}
