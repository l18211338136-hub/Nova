using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.EventBus.Outbox;

namespace Nova.Framework.Jobs;

public static class JobsExtensions
{
    /// <summary>
    /// 注册 Hangfire 后台任务服务，使用 PostgreSQL 作为持久化存储
    /// </summary>
    public static IServiceCollection AddNovaJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");


        // 为公网/长驻后台任务自动补齐 TCP KeepAlive 与空闲连接淘汰，防止云端 NAT 静默断流
        var connBuilder = new Npgsql.NpgsqlConnectionStringBuilder(rawConnectionString)
        {
            KeepAlive = 30,                 // 每 30 秒发送 TCP KeepAlive 探测包
            ConnectionIdleLifetime = 60,    // 空闲连接超过 60 秒主动回收，避免拿到死连接
            ConnectionPruningInterval = 10, // 每 10 秒清理一次过期空闲连接
        };


        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                c => c.UseNpgsqlConnection(connBuilder.ConnectionString),
                new PostgreSqlStorageOptions
                {
                    DistributedLockTimeout = TimeSpan.FromMinutes(1),
                    QueuePollInterval = TimeSpan.FromSeconds(5),
                }));

        // 启动 Hangfire Server（后台任务执行器）
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5; // 并发工作线程数
            options.Queues = ["default", "critical"];
        });

        return services;
    }

    /// <summary>
    /// 挂载 Hangfire Dashboard（后台任务管理界面）
    /// 路径：/jobs/dashboard
    /// </summary>
    public static IApplicationBuilder UseNovaJobs(this IApplicationBuilder app, bool requireAuth = true)
    {
        var dashboardOptions = new DashboardOptions
        {
            DashboardTitle = "Nova 后台任务管理",
            // 生产环境建议接入权限控制；开发环境允许所有人访问
            Authorization = requireAuth
                ? [new NovaHangfireDashboardAuthFilter()]
                : [new AllowAllDashboardAuthorizationFilter()],
        };

        app.UseHangfireDashboard("/jobs/dashboard", dashboardOptions);

        // 自动挂载事件总线的 Outbox 发件箱处理器后台任务 (每10秒执行一次)
        try
        {
            RecurringJob.AddOrUpdate<OutboxProcessorJob>(
                "nova-outbox-processor", 
                job => job.ProcessPendingMessagesAsync(), 
                "*/10 * * * * *");
        }
        catch { }

        return app;
    }
}
