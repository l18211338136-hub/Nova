using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Framework.Web.Modular;
using Nova.Framework.Persistence.Extensions;
using Nova.Framework.Persistence.Interceptors;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Application.Dtos;
using Nova.Modules.Mcp.Infrastructure.Persistence;
using Nova.Modules.Mcp.Infrastructure.OpenApi.Generator;
using Nova.Modules.Mcp.Infrastructure.Providers.Http;
using Nova.Modules.Mcp.Infrastructure.Server.Engine;
using Nova.Modules.Mcp.Infrastructure.Services;

namespace Nova.Modules.MCP.Api;

public class MCPModule : IModule
{
    public string Name => "MCP";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<UtcDateTimeParameterInterceptor>();

        // MCP 使用独立数据库（nova_mcp_db）：所有租户共享同一物理库，
        // 靠 McpServer/McpTool/McpKey 上的 TenantId 列做逻辑隔离（实体已 IsMultiTenant）。
        // 未配置 McpConnection 时回退到 DefaultConnection，保证本地/旧环境仍可运行。
        services.AddDbContext<McpDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("McpConnection")
                ?? configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString);
            options.ReplaceService<IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();

            options.AddNovaInterceptors(sp);
        });

        services.AddScoped<IMcpDbContext>(sp => sp.GetRequiredService<McpDbContext>());

        // 注册 MCP 核心服务
        services.AddTransient<IOpenApiToolGenerator, OpenApiToolGenerator>();
        services.AddTransient<HttpToolExecutor>();
        // 引擎必须注册为单例：SSE 长连接（HoldSseConnectionAsync）与
        // POST /messages（HandleMessageAsync）是两个独立的 HTTP 请求，
        // 会话字典 _activeSessions 必须跨请求共享，否则 POST 找不到 SSE 注册的
        // channel 会返回 false → 400。引擎已移除 HttpContext 捕获，改为通过
        // IServiceScopeFactory 在每次处理时新建 Scope 解析 IMcpDbContext，
        // 因此单例是安全的（无 per-request 状态泄漏）。
        services.AddSingleton<IMcpServerEngine, McpServerEngine>();

        // Application 层契约 → Infrastructure 层实现（依赖倒置）
        services.AddScoped<IMcpKeyService, McpKeyService>();
        services.AddScoped<IMcpPagedQueryService<McpKeyDto>, McpKeyQueryService>();
        services.AddScoped<IMcpPagedQueryService<McpServerDto>, McpServerQueryService>();
        services.AddScoped<IMcpPagedQueryService<McpToolDto>, McpToolQueryService>();

        // 注册 HttpToolExecutor 专用的 HttpClient（工具执行，超时保持较短）
        services.AddHttpClient("McpDynamicClient", client => 
        {
            client.Timeout = TimeSpan.FromSeconds(30); 
        });

        // 注册 Swagger 文档拉取专用的 HttpClient：文档可能数兆且伴随网络延迟，
        // 超时放宽到 5 分钟，避免大文档在下载途中被中断（不复用 McpDynamicClient 的 30s）。
        services.AddHttpClient("McpSwaggerFetchClient", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // 密钥查询已改为 MVC 控制器（Controllers/McpKeyController），
        // 由框架的 MapModuleEndpoints() 统一 MapControllers() 发现，无需在此注册。
    }
}
