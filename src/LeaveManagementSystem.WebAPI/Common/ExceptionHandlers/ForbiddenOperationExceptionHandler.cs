using System.Diagnostics;
using LeaveManagementSystem.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.WebAPI.Common.ExceptionHandlers;

public sealed class ForbiddenOperationExceptionHandler
    : IExceptionHandler
{
    private const string ForbiddenTitle =
        "Forbidden.";

    private const string ForbiddenDetail =
        "You do not have permission to perform this operation.";

    private readonly IProblemDetailsService
        _problemDetailsService;

    public ForbiddenOperationExceptionHandler(
        IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService =
            problemDetailsService
            ?? throw new ArgumentNullException(
                nameof(problemDetailsService));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ForbiddenOperationException)
        {
            return false;
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
                    problemDetails,

                Exception =
                    exception
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
                cancellationToken: cancellationToken);
        }

        return true;
    }
}
