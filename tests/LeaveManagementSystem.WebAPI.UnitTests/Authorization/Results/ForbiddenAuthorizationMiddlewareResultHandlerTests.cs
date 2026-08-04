using System.Security.Claims;
using System.Text.Json;
using LeaveManagementSystem.WebAPI.Authorization.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.WebAPI.UnitTests.Authorization.Results;

public sealed class ForbiddenAuthorizationMiddlewareResultHandlerTests
{
    [Fact]
    public void Constructor_WithNullProblemDetailsService_ThrowsArgumentNullException()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () =>
                    new ForbiddenAuthorizationMiddlewareResultHandler(
                        null!));

        Assert.Equal(
            "problemDetailsService",
            exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WithSuccessfulResult_InvokesNextDelegate()
    {
        var problemDetailsService =
            new FakeProblemDetailsService();

        var authenticationService =
            new FakeAuthenticationService();

        using var serviceProvider =
            CreateServiceProvider(
                authenticationService);

        var handler =
            new ForbiddenAuthorizationMiddlewareResultHandler(
                problemDetailsService);

        var httpContext =
            CreateHttpContext(
                serviceProvider,
                "/api/test/success");

        var nextCallCount =
            0;

        RequestDelegate next =
            context =>
            {
                nextCallCount++;

                context.Response.StatusCode =
                    StatusCodes.Status204NoContent;

                return Task.CompletedTask;
            };

        await handler.HandleAsync(
            next,
            httpContext,
            CreatePolicy(),
            PolicyAuthorizationResult.Success());

        Assert.Equal(
            1,
            nextCallCount);

        Assert.Equal(
            StatusCodes.Status204NoContent,
            httpContext.Response.StatusCode);

        Assert.Empty(
            authenticationService.ChallengeSchemes);

        Assert.Empty(
            authenticationService.ForbidSchemes);

        Assert.Equal(
            0,
            problemDetailsService.TryWriteCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithChallengeResult_DelegatesToDefaultHandler()
    {
        var problemDetailsService =
            new FakeProblemDetailsService();

        var authenticationService =
            new FakeAuthenticationService();

        using var serviceProvider =
            CreateServiceProvider(
                authenticationService);

        var handler =
            new ForbiddenAuthorizationMiddlewareResultHandler(
                problemDetailsService);

        var httpContext =
            CreateHttpContext(
                serviceProvider,
                "/api/test/challenge");

        var nextCallCount =
            0;

        RequestDelegate next =
            _ =>
            {
                nextCallCount++;

                return Task.CompletedTask;
            };

        await handler.HandleAsync(
            next,
            httpContext,
            CreatePolicy(
                "TestScheme"),
            PolicyAuthorizationResult.Challenge());

        Assert.Equal(
            0,
            nextCallCount);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            httpContext.Response.StatusCode);

        Assert.Equal(
            new[] { "TestScheme" },
            authenticationService.ChallengeSchemes);

        Assert.Empty(
            authenticationService.ForbidSchemes);

        Assert.Equal(
            0,
            problemDetailsService.TryWriteCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithForbiddenResultAndNoPolicyScheme_WritesSafeProblemDetails()
    {
        var problemDetailsService =
            new FakeProblemDetailsService
            {
                TryWriteResult =
                    false
            };

        var authenticationService =
            new FakeAuthenticationService();

        using var serviceProvider =
            CreateServiceProvider(
                authenticationService);

        var handler =
            new ForbiddenAuthorizationMiddlewareResultHandler(
                problemDetailsService);

        const string requestPath =
            "/api/test/forbidden";

        var httpContext =
            CreateHttpContext(
                serviceProvider,
                requestPath);

        var nextCallCount =
            0;

        RequestDelegate next =
            _ =>
            {
                nextCallCount++;

                return Task.CompletedTask;
            };

        await handler.HandleAsync(
            next,
            httpContext,
            CreatePolicy(),
            PolicyAuthorizationResult.Forbid());

        Assert.Equal(
            0,
            nextCallCount);

        Assert.Empty(
            authenticationService.ChallengeSchemes);

        Assert.Null(
            Assert.Single(
                authenticationService.ForbidSchemes));

        Assert.Equal(
            1,
            problemDetailsService.TryWriteCallCount);

        var receivedContext =
            Assert.Single(
                problemDetailsService.ReceivedContexts);

        Assert.Same(
            httpContext,
            receivedContext.HttpContext);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            httpContext.Response.StatusCode);

        Assert.StartsWith(
            "application/problem+json",
            httpContext.Response.ContentType,
            StringComparison.OrdinalIgnoreCase);

        httpContext.Response.Body.Position =
            0;

        using var jsonDocument =
            await JsonDocument.ParseAsync(
                httpContext.Response.Body);

        var root =
            jsonDocument.RootElement;

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            root.GetProperty("status").GetInt32());

        Assert.Equal(
            "Forbidden.",
            root.GetProperty("title").GetString());

        Assert.Equal(
            "You do not have permission to perform this operation.",
            root.GetProperty("detail").GetString());

        Assert.Equal(
            requestPath,
            root.GetProperty("instance").GetString());

        var traceId =
            root.GetProperty("traceId").GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(
                traceId));
    }

    [Fact]
    public async Task HandleAsync_WithForbiddenResultAndMultiplePolicySchemes_ForbidsEveryScheme()
    {
        var problemDetailsService =
            new FakeProblemDetailsService
            {
                TryWriteResult =
                    true
            };

        var authenticationService =
            new FakeAuthenticationService();

        using var serviceProvider =
            CreateServiceProvider(
                authenticationService);

        var handler =
            new ForbiddenAuthorizationMiddlewareResultHandler(
                problemDetailsService);

        var httpContext =
            CreateHttpContext(
                serviceProvider,
                "/api/test/multiple-schemes");

        var nextCallCount =
            0;

        RequestDelegate next =
            _ =>
            {
                nextCallCount++;

                return Task.CompletedTask;
            };

        await handler.HandleAsync(
            next,
            httpContext,
            CreatePolicy(
                "FirstScheme",
                "SecondScheme"),
            PolicyAuthorizationResult.Forbid());

        Assert.Equal(
            0,
            nextCallCount);

        Assert.Equal(
            new[]
            {
                "FirstScheme",
                "SecondScheme"
            },
            authenticationService.ForbidSchemes);

        Assert.Empty(
            authenticationService.ChallengeSchemes);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            httpContext.Response.StatusCode);

        Assert.Equal(
            1,
            problemDetailsService.TryWriteCallCount);

        Assert.Equal(
            0,
            httpContext.Response.Body.Length);
    }

    private static AuthorizationPolicy CreatePolicy(
        params string[] authenticationSchemes)
    {
        var builder =
            new AuthorizationPolicyBuilder();

        if (authenticationSchemes.Length > 0)
        {
            builder.AddAuthenticationSchemes(
                authenticationSchemes);
        }

        builder.RequireAuthenticatedUser();

        return builder.Build();
    }

    private static ServiceProvider CreateServiceProvider(
        IAuthenticationService authenticationService)
    {
        var services =
            new ServiceCollection();

        services.AddSingleton(
            authenticationService);

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext(
        IServiceProvider serviceProvider,
        string requestPath)
    {
        var httpContext =
            new DefaultHttpContext
            {
                RequestServices =
                    serviceProvider
            };

        httpContext.Request.Path =
            requestPath;

        httpContext.Response.Body =
            new MemoryStream();

        return httpContext;
    }

    private sealed class FakeProblemDetailsService
        : IProblemDetailsService
    {
        public bool TryWriteResult { get; set; }

        public int TryWriteCallCount { get; private set; }

        public List<ProblemDetailsContext> ReceivedContexts { get; } =
            new();

        public ValueTask<bool> TryWriteAsync(
            ProblemDetailsContext context)
        {
            ArgumentNullException.ThrowIfNull(
                context);

            TryWriteCallCount++;

            ReceivedContexts.Add(
                context);

            return ValueTask.FromResult(
                TryWriteResult);
        }

        public ValueTask WriteAsync(
            ProblemDetailsContext context)
        {
            ArgumentNullException.ThrowIfNull(
                context);

            throw new InvalidOperationException(
                "Unexpected problem details write call.");
        }
    }

    private sealed class FakeAuthenticationService
        : IAuthenticationService
    {
        public List<string?> ChallengeSchemes { get; } =
            new();

        public List<string?> ForbidSchemes { get; } =
            new();

        public Task<AuthenticateResult> AuthenticateAsync(
            HttpContext context,
            string? scheme)
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            ChallengeSchemes.Add(
                scheme);

            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return Task.CompletedTask;
        }

        public Task ForbidAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            ForbidSchemes.Add(
                scheme);

            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            return Task.CompletedTask;
        }

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties)
        {
            throw new InvalidOperationException(
                "Unexpected sign-in call.");
        }

        public Task SignOutAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            throw new InvalidOperationException(
                "Unexpected sign-out call.");
        }
    }
}
