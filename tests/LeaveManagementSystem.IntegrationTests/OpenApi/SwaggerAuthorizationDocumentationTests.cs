using System.Net;
using System.Text.Json;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.OpenApi;

public sealed class SwaggerAuthorizationDocumentationTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    private const string SwaggerDocumentPath =
        "/swagger/v1/swagger.json";

    [Fact]
    public async Task SwaggerDocument_DefinesBearerSecurityScheme()
    {
        using var document =
            await GetSwaggerDocumentAsync();

        var bearerScheme =
            document.RootElement
                .GetProperty("components")
                .GetProperty("securitySchemes")
                .GetProperty("bearer");

        Assert.Equal(
            "http",
            bearerScheme
                .GetProperty("type")
                .GetString());

        Assert.Equal(
            "bearer",
            bearerScheme
                .GetProperty("scheme")
                .GetString());

        Assert.Equal(
            "JWT",
            bearerScheme
                .GetProperty("bearerFormat")
                .GetString());
    }

    [Fact]
    public async Task SwaggerDocument_LoginEndpoint_DoesNotRequireBearerSecurity()
    {
        using var document =
            await GetSwaggerDocumentAsync();

        var operation =
            GetOperation(
                document,
                "/api/auth/login",
                "post");

        Assert.False(
            operation.TryGetProperty(
                "security",
                out _));
    }

    [Fact]
    public async Task SwaggerDocument_PublicHealthEndpoint_DoesNotRequireBearerSecurity()
    {
        using var document =
            await GetSwaggerDocumentAsync();

        var operation =
            GetOperation(
                document,
                "/api/health",
                "get");

        Assert.False(
            operation.TryGetProperty(
                "security",
                out _));
    }

    [Fact]
    public async Task SwaggerDocument_ProtectedEndpoint_RequiresBearerSecurity()
    {
        using var document =
            await GetSwaggerDocumentAsync();

        var operation =
            GetOperation(
                document,
                "/api/test-authentication/authenticated-employee",
                "get");

        var securityRequirements =
            operation.GetProperty(
                "security");

        var securityRequirement =
            Assert.Single(
                securityRequirements
                    .EnumerateArray()
                    .ToArray());

        Assert.True(
            securityRequirement.TryGetProperty(
                "bearer",
                out var requiredScopes));

        Assert.Equal(
            JsonValueKind.Array,
            requiredScopes.ValueKind);

        Assert.Equal(
            0,
            requiredScopes.GetArrayLength());
    }

    private async Task<JsonDocument> GetSwaggerDocumentAsync()
    {
        using var response =
            await _client.GetAsync(
                SwaggerDocumentPath);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        await using var responseStream =
            await response.Content
                .ReadAsStreamAsync();

        return await JsonDocument.ParseAsync(
            responseStream);
    }

    private static JsonElement GetOperation(
    JsonDocument document,
    string expectedPath,
    string httpMethod)
    {
        var paths =
            document.RootElement
                .GetProperty("paths");

        var path =
            Assert.Single(
                paths.EnumerateObject(),
                property =>
                    string.Equals(
                        property.Name,
                        expectedPath,
                        StringComparison.OrdinalIgnoreCase));

        return path.Value.GetProperty(
            httpMethod);
    }
}
