using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nova.Framework.Jobs;

public static class JobsExtensions
{
    /// <summary>
    /// 注册 Hangfire 后台任务服务，使用 PostgreSQL 作为持久化存储
    /// </summary>
    public static IServiceCollection AddNovaJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

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

        return app;
    }
}
