using FluentValidation;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.TestSupport;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LeaveManagementSystem.IntegrationTests;

public sealed class TestWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=leave_management_test_db;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(TestConnectionString));

            services
                .AddControllers()
                .AddApplicationPart(
                    typeof(TestValidationController).Assembly);

            services.AddTransient<
                IValidator<TestValidationCommand>,
                TestValidationCommandValidator>();

            services.AddTransient<
                IRequestHandler<TestValidationCommand, string>,
                TestValidationCommandHandler>();
        });
    }
}
