using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Nova.Framework.Web.OpenApi;

public class TenantHeaderOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Id",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Tenant Identifier (e.g. tenant1, tenant2)",
            Schema = new OpenApiSchema
            {
                Type = "string"
            }
        });

        return Task.CompletedTask;
    }
}
