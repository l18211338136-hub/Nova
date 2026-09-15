using System;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Framework.Web.Modular;
using Nova.Framework.Persistence.Extensions;
using Nova.Framework.Persistence.Interceptors;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Infrastructure.Persistence;
using Nova.Modules.Mcp.Infrastructure.OpenApi.Generator;
using Nova.Modules.Mcp.Infrastructure.Providers.Http;
using Nova.Modules.Mcp.Infrastructure.Server.Engine;

namespace Nova.Modules.MCP.Api;

public class MCPModule : IModule
{
    public string Name => "MCP";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<UtcDateTimeParameterInterceptor>();

        services.AddDbContext<McpDbContext>((sp, options) =>
        {
            var tenantInfo = sp.GetRequiredService<IMultiTenantContextAccessor>().MultiTenantContext?.TenantInfo as NovaTenantInfo;
            var connectionString = tenantInfo?.ConnectionString
                ?? configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString);
            options.ReplaceService<IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();

            options.AddNovaInterceptors(sp);
        });

        services.AddScoped<IMcpDbContext>(sp => sp.GetRequiredService<McpDbContext>());

        // 注册 MCP 核心服务
        services.AddTransient<IOpenApiToolGenerator, OpenApiToolGenerator>();
        services.AddTransient<HttpToolExecutor>();
        services.AddSingleton<IMcpServerEngine, McpServerEngine>();

        // 注册 HttpToolExecutor 专用的 HttpClient
        services.AddHttpClient("McpDynamicClient", client => 
        {
            client.Timeout = TimeSpan.FromSeconds(30); 
        });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Controllers are automatically mapped by the framework
    }
}
