using LeaveManagementSystem.Application;
using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Constants;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure;
using LeaveManagementSystem.Infrastructure.DemoData;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.Infrastructure.Authentication.Jwt;
using LeaveManagementSystem.WebAPI.Authentication.CurrentUser;
using LeaveManagementSystem.WebAPI.Authentication.Jwt;
using LeaveManagementSystem.WebAPI.Authorization.Handlers;
using LeaveManagementSystem.WebAPI.Authorization.Policies;
using LeaveManagementSystem.WebAPI.Authorization.Requirements;
using LeaveManagementSystem.WebAPI.Authorization.Results;
using LeaveManagementSystem.WebAPI.Common.ExceptionHandlers;
using LeaveManagementSystem.WebAPI.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Data.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddSingleton<RequiredJwtClaimsValidator>();
builder.Services.AddScoped<RequiredJwtBearerEvents>();

builder.Services.AddScoped<
    IAuthorizationHandler,
    CurrentUserAccessAuthorizationHandler>();

builder.Services.AddSingleton<
    IAuthorizationMiddlewareResultHandler,
    ForbiddenAuthorizationMiddlewareResultHandler>();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "Enter the JWT access token."
        });

    options.OperationFilter<
        BearerSecurityRequirementOperationFilter>();
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();

builder.Services.AddExceptionHandler<
    ForbiddenOperationExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgresql");

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(
        JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>(
        (bearerOptions, jwtOptionsAccessor) =>
        {
            var jwtOptions =
                jwtOptionsAccessor.Value;

            bearerOptions.IncludeErrorDetails =
                false;

            bearerOptions.EventsType =
                typeof(RequiredJwtBearerEvents);

            bearerOptions.MapInboundClaims =
                false;

            bearerOptions.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                jwtOptions.SigningKey)),

                    ValidAlgorithms =
                        new[] { SecurityAlgorithms.HmacSha256 },

                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero,

                    NameClaimType =
                        JwtRegisteredClaimNames.Email,

                    RoleClaimType =
                        JwtClaimNames.Role
                };
        });

builder.Services.AddAuthorization(options =>
{
    var authenticatedEmployeePolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(
                new CurrentUserAccessRequirement())
            .Build();

    options.DefaultPolicy =
        authenticatedEmployeePolicy;

    options.FallbackPolicy =
        authenticatedEmployeePolicy;

    options.AddPolicy(
        AuthorizationPolicyNames.AuthenticatedEmployee,
        authenticatedEmployeePolicy);

    options.AddPolicy(
        AuthorizationPolicyNames.HrOnly,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new CurrentUserAccessRequirement(
                    EmployeeRole.HR));
        });

    options.AddPolicy(
        AuthorizationPolicyNames.ManagerOnly,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new CurrentUserAccessRequirement(
                    EmployeeRole.Manager));
        });
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>(
        "DatabaseInitialization:ApplyMigrationsOnStartup"))
{
    await ApplyMigrationsWithRetryAsync(app);
}

if (app.Environment.IsDevelopment())
{
    using var scope =
        app.Services.CreateScope();

    var demoDataSeeder =
        scope.ServiceProvider
            .GetRequiredService<DemoDataSeeder>();

    await demoDataSeeder.SeedAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task ApplyMigrationsWithRetryAsync(
    WebApplication app)
{
    const int maxAttempts = 5;

    var retryDelay =
        TimeSpan.FromSeconds(5);

    for (var attempt = 1;
         attempt <= maxAttempts;
         attempt++)
    {
        try
        {
            using var scope =
                app.Services.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            await dbContext.Database.MigrateAsync();

            return;
        }
        catch (Exception exception)
            when (
                attempt < maxAttempts &&
                exception is DbException or TimeoutException)
        {
            app.Logger.LogWarning(
                exception,
                "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds} seconds.",
                attempt,
                maxAttempts,
                retryDelay.TotalSeconds);

            await Task.Delay(retryDelay);
        }
    }
}

public partial class Program
{
}
