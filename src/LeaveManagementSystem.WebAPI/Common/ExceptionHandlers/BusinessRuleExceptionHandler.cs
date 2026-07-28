using System.Diagnostics;
using LeaveManagementSystem.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagementSystem.WebAPI.Common.ExceptionHandlers;

public sealed class BusinessRuleExceptionHandler : IExceptionHandler
{
    private const string BusinessRuleErrorTitle =
        "A business rule was violated.";

    private readonly IProblemDetailsService _problemDetailsService;

    public BusinessRuleExceptionHandler(
        IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException businessRuleException)
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = BusinessRuleErrorTitle,
            Detail = businessRuleException.Message,
            Instance = httpContext.Request.Path.Value
        };

        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode =
            StatusCodes.Status400BadRequest;

        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        };

        var wasWritten = await _problemDetailsService.TryWriteAsync(
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
