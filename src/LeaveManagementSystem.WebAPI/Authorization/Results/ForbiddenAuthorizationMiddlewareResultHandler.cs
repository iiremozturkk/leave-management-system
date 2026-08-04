using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.WebAPI.Authorization.Results;

public sealed class ForbiddenAuthorizationMiddlewareResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private const string ForbiddenTitle =
        "Forbidden.";

    private const string ForbiddenDetail =
        "You do not have permission to perform this operation.";

    private static readonly AuthorizationMiddlewareResultHandler
        DefaultHandler =
            new();

    private readonly IProblemDetailsService
        _problemDetailsService;

    public ForbiddenAuthorizationMiddlewareResultHandler(
        IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService =
            problemDetailsService
            ?? throw new ArgumentNullException(
                nameof(problemDetailsService));
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext httpContext,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Forbidden)
        {
            await DefaultHandler.HandleAsync(
                next,
                httpContext,
                policy,
                authorizeResult);

            return;
        }

        await ForbidAsync(
            httpContext,
            policy);

        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var problemDetails =
            new ProblemDetails
            {
                Status =
                    StatusCodes.Status403Forbidden,

                Title =
                    ForbiddenTitle,

                Detail =
                    ForbiddenDetail,

                Instance =
                    httpContext.Request.Path.Value
            };

        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id
            ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode =
            StatusCodes.Status403Forbidden;

        var problemDetailsContext =
            new ProblemDetailsContext
            {
                HttpContext =
                    httpContext,

                ProblemDetails =
                    problemDetails
            };

        var wasWritten =
            await _problemDetailsService.TryWriteAsync(
                problemDetailsContext);

        if (!wasWritten)
        {
            await httpContext.Response.WriteAsJsonAsync(
                value: problemDetails,
                options: null,
                contentType: "application/problem+json",
                cancellationToken:
                    httpContext.RequestAborted);
        }
    }

    private static async Task ForbidAsync(
        HttpContext httpContext,
        AuthorizationPolicy policy)
    {
        if (policy.AuthenticationSchemes.Count == 0)
        {
            await httpContext.ForbidAsync();

            return;
        }

        foreach (var authenticationScheme
                 in policy.AuthenticationSchemes)
        {
            await httpContext.ForbidAsync(
                authenticationScheme);
        }
    }
}
