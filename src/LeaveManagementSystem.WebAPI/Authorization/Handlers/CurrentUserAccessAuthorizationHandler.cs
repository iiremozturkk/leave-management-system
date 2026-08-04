using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.WebAPI.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace LeaveManagementSystem.WebAPI.Authorization.Handlers;

public sealed class CurrentUserAccessAuthorizationHandler
    : AuthorizationHandler<CurrentUserAccessRequirement>
{
    private readonly ICurrentUserAccessService
        _currentUserAccessService;

    private readonly IHttpContextAccessor
        _httpContextAccessor;

    public CurrentUserAccessAuthorizationHandler(
        ICurrentUserAccessService currentUserAccessService,
        IHttpContextAccessor httpContextAccessor)
    {
        _currentUserAccessService =
            currentUserAccessService
            ?? throw new ArgumentNullException(
                nameof(currentUserAccessService));

        _httpContextAccessor =
            httpContextAccessor
            ?? throw new ArgumentNullException(
                nameof(httpContextAccessor));
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CurrentUserAccessRequirement requirement)
    {
        var cancellationToken =
            _httpContextAccessor
                .HttpContext?
                .RequestAborted
            ?? CancellationToken.None;

        var currentAccess =
            await _currentUserAccessService.GetAsync(
                cancellationToken);

        if (currentAccess is null)
        {
            return;
        }

        if (requirement.RequiredRole is not null
            && currentAccess.Role
                != requirement.RequiredRole.Value)
        {
            return;
        }

        context.Succeed(
            requirement);
    }
}
