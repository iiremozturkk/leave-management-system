using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LeaveManagementSystem.WebAPI.OpenApi;

public sealed class BearerSecurityRequirementOperationFilter
    : IOperationFilter
{
    private const string BearerSecuritySchemeName =
        "bearer";

    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(
            operation);

        ArgumentNullException.ThrowIfNull(
            context);

        var endpointMetadata =
            context.ApiDescription
                .ActionDescriptor
                .EndpointMetadata;

        var allowsAnonymous =
            endpointMetadata
                .OfType<IAllowAnonymous>()
                .Any();

        if (allowsAnonymous)
        {
            return;
        }

        var requiresAuthorization =
            endpointMetadata
                .OfType<IAuthorizeData>()
                .Any();

        if (!requiresAuthorization)
        {
            return;
        }

        operation.Security ??=
            [];

        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(
                        BearerSecuritySchemeName,
                        context.Document)
                ] =
                    []
            });
    }
}
