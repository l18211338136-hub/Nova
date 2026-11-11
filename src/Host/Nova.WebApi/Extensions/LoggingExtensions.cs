using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace Nova.WebApi.Extensions;

public static class LoggingExtensions
{
    /// <summary>
    /// 注册并配置 Nova 统一的 Serilog 文件与控制台日志系统。
    /// 日志按天切分输出至 Logs/ 目录，保留最近 7 天。
    /// </summary>
    public static WebApplicationBuilder AddNovaLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "Logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                );
        });

        return builder;
    }
}

