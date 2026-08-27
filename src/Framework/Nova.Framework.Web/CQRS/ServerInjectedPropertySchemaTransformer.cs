using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Nova.Contracts.CQRS;
using System.Reflection;

namespace Nova.Framework.Web.CQRS;

/// <summary>
/// 从命令对象的请求体 Schema 中剔除由框架自动注入的属性
/// （<see cref="ServerInjectedProperties.CurrentUserId"/> / <see cref="ServerInjectedProperties.CurrentTenantId"/>）。
/// </summary>
/// <remarks>
/// 这些属性在服务端由 JWT 填充，暴露到 OpenAPI 会让前端代码生成器产出多余（甚至必填）的入参，
/// 客户端如果真的传值也会被服务端覆盖，属于纯噪音。仅处理带 <see cref="ApiEndpointAttribute"/> 的命令类型，
/// 避免误伤同名的业务 DTO。
/// </remarks>
public class ServerInjectedPropertySchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        // 修复树形结构 DTO 自引用产生的非标准 JSON Pointer ($ref)
        if (schema.Properties != null && schema.Properties.TryGetValue("children", out var childrenSchema) && childrenSchema != null)
        {
            if (childrenSchema.Type == "array" && childrenSchema.Items != null)
            {
                childrenSchema.Items.UnresolvedReference = true;
                childrenSchema.Items.Reference = new OpenApiReference
                {
                    Type = ReferenceType.Schema,
                    Id = type.Name
                };
            }
        }

        if (schema.Properties is null || schema.Properties.Count == 0) return Task.CompletedTask;
        if (type.GetCustomAttribute<ApiEndpointAttribute>() is null) return Task.CompletedTask;

        foreach (var name in schema.Properties.Keys.ToList())
        {
            if (!ServerInjectedProperties.Contains(name)) continue;

            schema.Properties.Remove(name);
            schema.Required?.Remove(name);
        }

        return Task.CompletedTask;
    }
}
