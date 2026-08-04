using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Nova.Contracts.CQRS;
using System.Reflection;

namespace Nova.Framework.Web.CQRS;

public class AutoEndpointOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var route = context.Description.RelativePath;
        if (string.IsNullOrEmpty(route)) return Task.CompletedTask;

        var commandMetadata = context.Description.ActionDescriptor.EndpointMetadata.OfType<CommandTypeMetadata>().FirstOrDefault();
        if (commandMetadata == null) return Task.CompletedTask;

        var commandType = commandMetadata.CommandType;
        var method = context.Description.HttpMethod;

        var matches = System.Text.RegularExpressions.Regex.Matches(route, @"\{([^}:]+)(?::[^}]+)?\}");
        var routeParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. 补充 Path Parameters
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var paramName = match.Groups[1].Value;
            routeParams.Add(paramName);
            operation.Parameters ??= new List<OpenApiParameter>();
            
            if (!operation.Parameters.Any(p => p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase) && p.In == ParameterLocation.Path))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = paramName,
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" }
                });
            }
        }

        // 2. 补充 Query Parameters (仅针对 GET 和 DELETE)
        if (method != null && (method.Equals("GET", StringComparison.OrdinalIgnoreCase) || method.Equals("DELETE", StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters ??= new List<OpenApiParameter>();
            foreach (var prop in commandType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (routeParams.Contains(prop.Name)) continue; // 已作为路径参数提取

                // 由框架从 JWT 注入，客户端不需要（也不应）传递，不暴露到契约里
                if (ServerInjectedProperties.Contains(prop.Name)) continue;

                if (!operation.Parameters.Any(p => p.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase) && p.In == ParameterLocation.Query))
                {
                    var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    var paramType = "string";
                    var format = "";
                    
                    if (t == typeof(int) || t == typeof(long) || t == typeof(short)) paramType = "integer";
                    else if (t == typeof(bool)) paramType = "boolean";
                    else if (t == typeof(Guid)) { paramType = "string"; format = "uuid"; }

                    var camelCaseName = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);

                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = camelCaseName,
                        In = ParameterLocation.Query,
                        Required = Nullable.GetUnderlyingType(prop.PropertyType) == null && !prop.PropertyType.IsClass && prop.PropertyType != typeof(string),
                        Schema = new OpenApiSchema { Type = paramType, Format = format }
                    });
                }
            }
        }

        return Task.CompletedTask;
    }
}
