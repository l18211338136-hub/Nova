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
        RouteHandlerBuilder builder;

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            builder = endpoints.MapMethods(route, new[] { method.ToUpper() }, async ([AsParameters] TCommand command, IMediator mediator) =>
            {
                var client = mediator.CreateRequestClient<TCommand>();
                var response = await client.GetResponse<TResponse>(command);
                return Results.Ok(ApiResponse<TResponse>.Success(response.Message));
            });
        }
        else
        {
            builder = endpoints.MapMethods(route, new[] { method.ToUpper() }, async ([FromBody] TCommand command, IMediator mediator) =>
            {
                var client = mediator.CreateRequestClient<TCommand>();
                var response = await client.GetResponse<TResponse>(command);
                return Results.Ok(ApiResponse<TResponse>.Success(response.Message));
            });
        }

        builder.Produces<ApiResponse<TResponse>>(StatusCodes.Status200OK);

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
