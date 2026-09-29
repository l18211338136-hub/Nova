using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Nova.Framework.Web.OpenApi;

public class TenantHeaderOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        // McpGateway 端点（第三方 AI 客户端专用，仅凭 MCP Key 访问）不需要也不应要求租户 ID，
        // 因此不注入 X-Tenant-Id 必填头，避免文档与其「免租户访问」语义冲突。
        var isGateway = operation.Tags?.Any(t => t.Name == "McpGateway") == true
            || context.Description.RelativePath is { } relativePath
               && (relativePath.Contains("api/mcp/sse", StringComparison.OrdinalIgnoreCase)
                   || relativePath.Contains("api/mcp/messages", StringComparison.OrdinalIgnoreCase));
        if (isGateway)
        {
            return Task.CompletedTask;
        }

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
