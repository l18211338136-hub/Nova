using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Framework.Persistence.Interceptors;
using Nova.Framework.Web.Modular;
using Nova.Modules.Audit.Application.Database;
using Nova.Modules.Audit.Infrastructure.Persistence;
using Nova.Modules.Audit.Infrastructure.Services;

namespace Nova.Modules.Audit.Api;

public class AuditModule : IModule
{
    public string Name => "Audit";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<UtcDateTimeParameterInterceptor>();

        services.AddDbContext<AuditDbContext>((sp, options) =>
        {
            var tenantInfo = sp.GetRequiredService<IMultiTenantContextAccessor>().MultiTenantContext?.TenantInfo as NovaTenantInfo;
            var connectionString = tenantInfo?.ConnectionString
                ?? configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString);
            options.ReplaceService<IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();

            var interceptor = sp.GetService<AuditableEntitySaveChangesInterceptor>();
            if (interceptor != null)
            {
                options.AddInterceptors(interceptor);
            }

            options.AddInterceptors(sp.GetRequiredService<UtcDateTimeParameterInterceptor>());
        });

        services.AddScoped<IAuditDbContext>(sp => sp.GetRequiredService<AuditDbContext>());

        // 依赖框架 AddAutoDependencyInjection() 自动注册实现 ISingletonDependency 的服务
        // 仅显式挂载后台批处理 HostedService
        services.AddHostedService<LogProcessorHostedService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // 模块自定义端点（框架 AutoEndpoints 已自动通过 [ApiEndpoint] 映射 GetOperationLogsQuery）
    }
}
