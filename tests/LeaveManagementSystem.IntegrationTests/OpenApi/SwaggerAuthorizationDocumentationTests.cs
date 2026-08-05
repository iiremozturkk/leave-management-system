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

        AssertRequiresBearerSecurity(
            operation);
    }

    [Theory]
    [InlineData("/api/leave-requests/{id}/approve")]
    [InlineData("/api/leave-requests/{id}/reject")]
    public async Task SwaggerDocument_LeaveReviewEndpoint_RequiresBearerSecurity(
        string expectedPath)
    {
        using var document =
            await GetSwaggerDocumentAsync();

        var operation =
            GetOperation(
                document,
                expectedPath,
                "post");

        AssertRequiresBearerSecurity(
            operation);
    }

    [Theory]
    [InlineData("/api/leave-requests", "get")]
    [InlineData("/api/leave-requests/{id}", "get")]
    [InlineData("/api/leave-requests/balance", "get")]
    [InlineData("/api/leave-requests", "post")]
    [InlineData("/api/leave-requests/{id}", "put")]
    [InlineData("/api/leave-requests/{id}", "delete")]
    public async Task SwaggerDocument_LeaveSelfServiceEndpoint_RequiresBearerSecurity(
    string expectedPath,
    string httpMethod)
    {
        using var document =
            await GetSwaggerDocumentAsync();

        var operation =
            GetOperation(
                document,
                expectedPath,
                httpMethod);

        AssertRequiresBearerSecurity(
            operation);
    }

    [Fact]
    public async Task SwaggerDocument_LeaveSelfServiceContract_DoesNotExposeEmployeeIdentity()
    {
        using var document =
            await GetSwaggerDocumentAsync();

        var schemas =
            document.RootElement
                .GetProperty("components")
                .GetProperty("schemas");

        var createRequestSchema =
            schemas.GetProperty(
                "CreateLeaveRequestRequest");

        var createProperties =
            createRequestSchema.GetProperty(
                "properties");

        Assert.False(
            createProperties.TryGetProperty(
                "employeeId",
                out _));

        Assert.True(
            createProperties.TryGetProperty(
                "leaveTypeId",
                out _));

        Assert.True(
            createProperties.TryGetProperty(
                "startDate",
                out _));

        Assert.True(
            createProperties.TryGetProperty(
                "endDate",
                out _));

        Assert.True(
            createProperties.TryGetProperty(
                "reason",
                out _));

        var balanceOperation =
            GetOperation(
                document,
                "/api/leave-requests/balance",
                "get");

        var parameterNames =
            balanceOperation
                .GetProperty("parameters")
                .EnumerateArray()
                .Select(
                    parameter =>
                        parameter
                            .GetProperty("name")
                            .GetString())
                .ToArray();

        Assert.DoesNotContain(
            "employeeId",
            parameterNames);

        Assert.Contains(
            "leaveTypeId",
            parameterNames);

        Assert.Contains(
            "year",
            parameterNames);
    }

    [Fact]
    public async Task SwaggerDocument_ReviewRequestSchema_DoesNotExposeReviewerIdentity()
    {
        using var document =
            await GetSwaggerDocumentAsync();

        var schemas =
            document.RootElement
                .GetProperty("components")
                .GetProperty("schemas");

        var reviewRequestSchema =
            schemas.GetProperty(
                "ReviewLeaveRequestRequest");

        var properties =
            reviewRequestSchema.GetProperty(
                "properties");

        var property =
            Assert.Single(
                properties
                    .EnumerateObject()
                    .ToArray());

        Assert.Equal(
            "managerComment",
            property.Name);

        Assert.False(
            properties.TryGetProperty(
                "reviewerEmployeeId",
                out _));
    }

    private static void AssertRequiresBearerSecurity(
        JsonElement operation)
    {
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

    [Theory]
    [InlineData("/api/employees", "get")]
    [InlineData("/api/employees", "post")]
    [InlineData("/api/employees/{id}", "get")]
    [InlineData("/api/employees/{id}", "put")]
    [InlineData("/api/employees/{id}", "delete")]
    public async Task SwaggerDocument_EmployeeAdministrationEndpoint_RequiresBearerSecurity(
    string expectedPath,
    string httpMethod)
    {
        using var document =
            await GetSwaggerDocumentAsync();

        var operation =
            GetOperation(
                document,
                expectedPath,
                httpMethod);

        AssertRequiresBearerSecurity(
            operation);
    }
}
