using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Nova.Framework.Web.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddNovaHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConn = configuration["Cache:RedisConnectionString"] ?? "127.0.0.1:6379,abortConnect=false";
        var dbConn = configuration.GetConnectionString("DefaultConnection");

        var builder = services.AddHealthChecks();

        if (!string.IsNullOrEmpty(redisConn))
        {
            builder.AddRedis(redisConn, name: "redis");
        }

        if (!string.IsNullOrEmpty(dbConn))
        {
            builder.AddNpgSql(dbConn, name: "postgresql");
        }

        return services;
    }

    public static WebApplication UseNovaHealthChecks(this WebApplication app)
    {
        // 1. 存活探针 (Liveness Probe): 校验节点进程是否活着
        app.MapHealthChecks("/healthz/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = async (context, _) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"status\":\"Live\"}");
            }
        });

        // 2. 就绪探针 (Readiness Probe): 校验 Redis / DB 依赖是否已连通准备就绪
        app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
        {
            ResponseWriter = WriteJsonResponse
        });

        return app;
    }

    private static Task WriteJsonResponse(HttpContext context, HealthReport result)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = result.Status.ToString(),
            durationMs = result.TotalDuration.TotalMilliseconds,
            checks = result.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
