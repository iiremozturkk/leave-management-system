using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LeaveManagementSystem.IntegrationTests;

public sealed class BusinessRuleExceptionHandlerTests
    : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public BusinessRuleExceptionHandlerTests(
        TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
    }

    [Fact]
    public async Task BusinessRuleException_ReturnsProblemDetails()
    {
        var request = new
        {
            message = "Department does not exist."
        };

        using var response = await _client.PostAsJsonAsync(
            "/__test/business-rule",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetailsResponse>(
                    JsonOptions);

        Assert.NotNull(problem);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            problem!.Status);

        Assert.Equal(
            "A business rule was violated.",
            problem.Title);

        Assert.Equal(
            "Department does not exist.",
            problem.Detail);

        Assert.Equal(
            "/__test/business-rule",
            problem.Instance);

        Assert.False(
            string.IsNullOrWhiteSpace(problem.TraceId));
    }

    private sealed record ProblemDetailsResponse(
        string Title,
        string Detail,
        int Status,
        string? Instance,
        string TraceId);
}
