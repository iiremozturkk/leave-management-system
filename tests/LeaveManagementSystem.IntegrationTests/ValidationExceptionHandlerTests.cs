using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LeaveManagementSystem.IntegrationTests;

public sealed class ValidationExceptionHandlerTests
    : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public ValidationExceptionHandlerTests(
        TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
    }

    [Fact]
    public async Task InvalidMediatRRequest_ReturnsValidationProblemDetails()
    {
        var request = new
        {
            name = string.Empty
        };

        using var response = await _client.PostAsJsonAsync(
            "/__test/validation",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ValidationProblemDetailsResponse>(
                    JsonOptions);

        Assert.NotNull(problem);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            problem!.Status);

        Assert.Equal(
            "One or more validation errors occurred.",
            problem.Title);

        Assert.Equal(
            "/__test/validation",
            problem.Instance);

        Assert.True(
            problem.Errors.TryGetValue(
                "Name",
                out var messages));

        Assert.NotNull(messages);
        Assert.Contains("Name is required.", messages);

        Assert.False(
            string.IsNullOrWhiteSpace(problem.TraceId));
    }

    private sealed record ValidationProblemDetailsResponse(
        string Title,
        int Status,
        string? Instance,
        Dictionary<string, string[]> Errors,
        string TraceId);
}
