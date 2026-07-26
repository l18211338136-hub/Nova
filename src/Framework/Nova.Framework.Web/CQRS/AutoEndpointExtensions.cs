using System.Reflection;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nova.Contracts.CQRS;
using Microsoft.AspNetCore.Mvc;
using Nova.Framework.Web.Responses;
using Microsoft.AspNetCore.OData.Query;

namespace Nova.Framework.Web.CQRS;

public static class AutoEndpointExtensions
{
    public static void MapAutoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var directory = AppDomain.CurrentDomain.BaseDirectory;
        var dllFiles = Directory.GetFiles(directory, "Nova.Modules.*.dll");

        foreach (var file in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                var commandTypes = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<ApiEndpointAttribute>() != null);

                foreach (var commandType in commandTypes)
                {
                    try
                    {
                        var attr = commandType.GetCustomAttribute<ApiEndpointAttribute>();
                        if (attr == null) continue;

                        var responseType = attr.ResponseType;
                        
                        var methodInfo = typeof(AutoEndpointExtensions).GetMethod(nameof(MapEndpointGeneric), BindingFlags.NonPublic | BindingFlags.Static);
                        if (methodInfo != null)
                        {
                            var genericMethod = methodInfo.MakeGenericMethod(commandType, responseType);
                            genericMethod.Invoke(null, new object[] { endpoints, attr.Method, attr.Route, attr.Tag, attr.Summary, attr.Description });
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine($"[AutoEndpoint] Failed to map endpoint for {commandType.Name}: {innerEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoEndpoint] Failed to scan assembly {file}: {ex.Message}");
            }
        }
    }

    private static void MapEndpointGeneric<TCommand, TResponse>(IEndpointRouteBuilder endpoints, string method, string route, string tag, string summary, string description)
        where TCommand : class
        where TResponse : class
    {
        var builder = endpoints.MapMethods(route, new[] { method.ToUpper() }, async (HttpContext context, IMediator mediator) =>
        {
            TCommand? command = null;
            if (context.Request.HasJsonContentType())
            {
                try { command = await context.Request.ReadFromJsonAsync<TCommand>(); } catch { }
            }
            
            command ??= Activator.CreateInstance<TCommand>();

            // 1. 强行绑定路由参数
            foreach (var routeValue in context.Request.RouteValues)
            {
                var prop = typeof(TCommand).GetProperty(routeValue.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite && routeValue.Value != null)
                {
                    var valStr = routeValue.Value.ToString();
                    var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    try
                    {
                        if (t.IsEnum) prop.SetValue(command, Enum.Parse(t, valStr!, true));
                        else if (t == typeof(Guid)) prop.SetValue(command, Guid.Parse(valStr!));
                        else prop.SetValue(command, Convert.ChangeType(valStr, t));
                    }
                    catch { } // 忽略绑定异常，保持鲁棒性
                }
            }

            // 2. 强行绑定 Query 参数 (针对 GET / DELETE 等)
            foreach (var query in context.Request.Query)
            {
                var prop = typeof(TCommand).GetProperty(query.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite && query.Value.Count > 0)
                {
                    var valStr = query.Value.ToString();
                    var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    try
                    {
                        if (t.IsEnum) prop.SetValue(command, Enum.Parse(t, valStr, true));
                        else if (t == typeof(Guid)) prop.SetValue(command, Guid.Parse(valStr));
                        else prop.SetValue(command, Convert.ChangeType(valStr, t));
                    }
                    catch { }
                }
            }

            var client = mediator.CreateRequestClient<TCommand>();
            var response = await client.GetResponse<TResponse>(command!);
            return Results.Ok(ApiResponse<TResponse>.Success(response.Message));
        });

        // 3. 针对携带 Body 的请求，显式告知 OpenAPI 期望的 JSON Schema
        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) || 
            method.Equals("PUT", StringComparison.OrdinalIgnoreCase) || 
            method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
        {
            builder.Accepts<TCommand>("application/json");
        }

        // 4. 将 CommandType 作为 Metadata 附加到 Endpoint，供全局 OpenAPI Transformer 读取
        builder.WithMetadata(new CommandTypeMetadata(typeof(TCommand)));

        // 5. 权限验证
        var requirePermAttr = typeof(TCommand).GetCustomAttribute<Nova.Contracts.Security.RequirePermissionAttribute>();
        if (requirePermAttr != null)
        {
            builder.RequireAuthorization();
            builder.AddEndpointFilter(new Nova.Framework.Web.Security.PermissionFilter(requirePermAttr.Permission));
        }

        builder.Produces<ApiResponse<TResponse>>(StatusCodes.Status200OK);
        
        var operationName = typeof(TCommand).Name;
        if (operationName.EndsWith("Command")) operationName = operationName.Substring(0, operationName.Length - 7);
        else if (operationName.EndsWith("Query")) operationName = operationName.Substring(0, operationName.Length - 5);
        
        builder.WithName(operationName);

        if (!string.IsNullOrEmpty(tag))
        {
            builder.WithTags(tag);
        }
        if (!string.IsNullOrEmpty(summary))
        {
            builder.WithSummary(summary);
        }
        if (!string.IsNullOrEmpty(description))
        {
            builder.WithDescription(description);
        }
    }
}
