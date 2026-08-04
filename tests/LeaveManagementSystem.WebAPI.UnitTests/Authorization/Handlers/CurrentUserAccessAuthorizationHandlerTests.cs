using System.Security.Claims;
using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.WebAPI.Authorization.Handlers;
using LeaveManagementSystem.WebAPI.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace LeaveManagementSystem.WebAPI.UnitTests.Authorization.Handlers;

public sealed class CurrentUserAccessAuthorizationHandlerTests
{
    private static readonly Guid UserAccountId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

    private static readonly Guid EmployeeId =
        Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Constructor_WithNullCurrentUserAccessService_ThrowsArgumentNullException()
    {
        var httpContextAccessor =
            new HttpContextAccessor();

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new CurrentUserAccessAuthorizationHandler(
                    null!,
                    httpContextAccessor));

        Assert.Equal(
            "currentUserAccessService",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ThrowsArgumentNullException()
    {
        var currentUserAccessService =
            new FakeCurrentUserAccessService();

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new CurrentUserAccessAuthorizationHandler(
                    currentUserAccessService,
                    null!));

        Assert.Equal(
            "httpContextAccessor",
            exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenCurrentAccessIsNull_DoesNotSucceedRequirement()
    {
        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result =
                    null
            };

        var handler =
            CreateHandler(
                currentUserAccessService);

        var requirement =
            new CurrentUserAccessRequirement();

        var context =
            CreateAuthorizationContext(
                requirement);

        await handler.HandleAsync(
            context);

        Assert.False(
            context.HasSucceeded);

        Assert.False(
            context.HasFailed);

        Assert.Equal(
            1,
            currentUserAccessService.CallCount);
    }

    [Fact]
    public async Task HandleAsync_WithoutRequiredRoleAndValidAccess_SucceedsRequirement()
    {
        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result =
                    CreateCurrentUserAccess(
                        EmployeeRole.Employee)
            };

        var handler =
            CreateHandler(
                currentUserAccessService);

        var requirement =
            new CurrentUserAccessRequirement();

        var context =
            CreateAuthorizationContext(
                requirement);

        await handler.HandleAsync(
            context);

        Assert.True(
            context.HasSucceeded);

        Assert.False(
            context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_WithMatchingRequiredRole_SucceedsRequirement()
    {
        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result =
                    CreateCurrentUserAccess(
                        EmployeeRole.Manager)
            };

        var handler =
            CreateHandler(
                currentUserAccessService);

        var requirement =
            new CurrentUserAccessRequirement(
                EmployeeRole.Manager);

        var context =
            CreateAuthorizationContext(
                requirement);

        await handler.HandleAsync(
            context);

        Assert.True(
            context.HasSucceeded);

        Assert.False(
            context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_WithDifferentRequiredRole_DoesNotSucceedRequirement()
    {
        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result =
                    CreateCurrentUserAccess(
                        EmployeeRole.Employee)
            };

        var handler =
            CreateHandler(
                currentUserAccessService);

        var requirement =
            new CurrentUserAccessRequirement(
                EmployeeRole.HR);

        var context =
            CreateAuthorizationContext(
                requirement);

        await handler.HandleAsync(
            context);

        Assert.False(
            context.HasSucceeded);

        Assert.False(
            context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_WithHttpContext_PassesRequestAbortedToken()
    {
        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result =
                    CreateCurrentUserAccess(
                        EmployeeRole.Employee)
            };

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var httpContext =
            new DefaultHttpContext
            {
                RequestAborted =
                    cancellationTokenSource.Token
            };

        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext =
                    httpContext
            };

        var handler =
            new CurrentUserAccessAuthorizationHandler(
                currentUserAccessService,
                httpContextAccessor);

        var requirement =
            new CurrentUserAccessRequirement();

        var context =
            CreateAuthorizationContext(
                requirement);

        await handler.HandleAsync(
            context);

        Assert.Equal(
            cancellationTokenSource.Token,
            Assert.Single(
                currentUserAccessService.ReceivedCancellationTokens));
    }

    [Fact]
    public async Task HandleAsync_WithoutHttpContext_PassesCancellationTokenNone()
    {
        var currentUserAccessService =
            new FakeCurrentUserAccessService
            {
                Result =
                    CreateCurrentUserAccess(
                        EmployeeRole.Employee)
            };

        var handler =
            new CurrentUserAccessAuthorizationHandler(
                currentUserAccessService,
                new HttpContextAccessor());

        var requirement =
            new CurrentUserAccessRequirement();

        var context =
            CreateAuthorizationContext(
                requirement);

        await handler.HandleAsync(
            context);

        Assert.Equal(
            CancellationToken.None,
            Assert.Single(
                currentUserAccessService.ReceivedCancellationTokens));
    }

    private static CurrentUserAccessAuthorizationHandler CreateHandler(
        ICurrentUserAccessService currentUserAccessService)
    {
        return new CurrentUserAccessAuthorizationHandler(
            currentUserAccessService,
            new HttpContextAccessor
            {
                HttpContext =
                    new DefaultHttpContext()
            });
    }

    private static AuthorizationHandlerContext CreateAuthorizationContext(
        CurrentUserAccessRequirement requirement)
    {
        var principal =
            new ClaimsPrincipal(
                new ClaimsIdentity());

        return new AuthorizationHandlerContext(
            new[] { requirement },
            principal,
            resource: null);
    }

    private static CurrentUserAccess CreateCurrentUserAccess(
        EmployeeRole role)
    {
        return new CurrentUserAccess(
            UserAccountId,
            EmployeeId,
            "authorization.handler@example.com",
            role);
    }

    private sealed class FakeCurrentUserAccessService
        : ICurrentUserAccessService
    {
        public CurrentUserAccess? Result { get; set; }

        public int CallCount { get; private set; }

        public List<CancellationToken> ReceivedCancellationTokens { get; } =
            new();

        public Task<CurrentUserAccess?> GetAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            ReceivedCancellationTokens.Add(
                cancellationToken);

            return Task.FromResult(
                Result);
        }
    }
}
