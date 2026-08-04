using System.Text.Json;
using LeaveManagementSystem.Application.Common.Exceptions;
using LeaveManagementSystem.WebAPI.Common.ExceptionHandlers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.WebAPI.UnitTests.Common.ExceptionHandlers;

public sealed class ForbiddenOperationExceptionHandlerTests
{
    [Fact]
    public void Constructor_WithNullProblemDetailsService_ThrowsArgumentNullException()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new ForbiddenOperationExceptionHandler(
                    null!));

        Assert.Equal(
            "problemDetailsService",
            exception.ParamName);
    }

    [Fact]
    public async Task TryHandleAsync_WithDifferentException_ReturnsFalseWithoutWritingResponse()
    {
        using var serviceProvider =
            CreateServiceProvider();

        var problemDetailsService =
            serviceProvider.GetRequiredService<
                IProblemDetailsService>();

        var handler =
            new ForbiddenOperationExceptionHandler(
                problemDetailsService);

        var httpContext =
            CreateHttpContext(
                "/api/test/non-forbidden");

        var handled =
            await handler.TryHandleAsync(
                httpContext,
                new InvalidOperationException(
                    "A different exception."),
                CancellationToken.None);

        Assert.False(
            handled);

        Assert.Equal(
            StatusCodes.Status200OK,
            httpContext.Response.StatusCode);

        Assert.Equal(
            0,
            httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task TryHandleAsync_WithForbiddenOperationException_WritesSafeForbiddenProblemDetails()
    {
        using var serviceProvider =
            CreateServiceProvider();

        var problemDetailsService =
            serviceProvider.GetRequiredService<
                IProblemDetailsService>();

        var handler =
            new ForbiddenOperationExceptionHandler(
                problemDetailsService);

        const string requestPath =
            "/api/test/forbidden";

        const string sensitiveExceptionMessage =
            "The current employee is not the direct manager.";

        var httpContext =
            CreateHttpContext(
                requestPath);

        var handled =
            await handler.TryHandleAsync(
                httpContext,
                new ForbiddenOperationException(
                    sensitiveExceptionMessage),
                CancellationToken.None);

        Assert.True(
            handled);

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

        var responseJson =
            root.GetRawText();

        Assert.False(
            responseJson.Contains(
                sensitiveExceptionMessage,
                StringComparison.Ordinal));
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services =
            new ServiceCollection();

        services.AddOptions();
        services.AddProblemDetails();

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext(
        string requestPath)
    {
        var httpContext =
            new DefaultHttpContext();

        httpContext.Request.Path =
            requestPath;

        httpContext.Response.Body =
            new MemoryStream();

        return httpContext;
    }
}
