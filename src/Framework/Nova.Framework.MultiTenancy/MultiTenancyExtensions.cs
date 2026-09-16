using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Nova.Framework.Persistence.Interceptors;
using Nova.Contracts.Constants;

namespace Nova.Framework.MultiTenancy;

public static class MultiTenancyExtensions
{
    public static IServiceCollection AddNovaMultiTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        // 拦截器无状态，注册为单例供各 DbContext 复用
        services.TryAddSingleton<UtcDateTimeParameterInterceptor>();

        services.AddDbContext<NovaTenantDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString);
            options.ReplaceService<IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();
            // 规范化写入 timestamptz 的本地 DateTime 参数为 UTC（修复 OData 日期筛选报错）
            options.AddInterceptors(sp.GetRequiredService<UtcDateTimeParameterInterceptor>());

        });

        services.AddMultiTenant<NovaTenantInfo>()
            .WithClaimStrategy(TenantConstants.TenantIdClaimType)
            .WithDelegateStrategy(async context =>
            {
                if (context is HttpContext httpContext)
                {
                    if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
                    {
                        var headerValue = headerTenantId.ToString();
                        if (!string.IsNullOrWhiteSpace(headerValue))
                            return headerValue;
                    }
                        
                    if (httpContext.Request.Query.TryGetValue("tenantId", out var queryTenantId))
                    {
                        var queryValue = queryTenantId.ToString();
                        if (!string.IsNullOrWhiteSpace(queryValue))
                            return queryValue;
                    }

                    if (httpContext.User.Identity?.IsAuthenticated == true)
                    {
                        var tenantClaim = httpContext.User.FindFirst(TenantConstants.TenantIdClaimType)?.Value;
                        if (!string.IsNullOrWhiteSpace(tenantClaim))
                            return tenantClaim;
                    }

                }
                return null;
            })
            .WithHostStrategy()
            .WithStore<EFCoreStore<NovaTenantDbContext, NovaTenantInfo>>(ServiceLifetime.Scoped); 

        services.AddScoped<ITenantInfo>(sp => 
        {
            var accessor = sp.GetRequiredService<IMultiTenantContextAccessor<NovaTenantInfo>>();
            return accessor.MultiTenantContext?.TenantInfo!;
        });

        return services;
    }

    public static IApplicationBuilder UseNovaMultiTenancy(this IApplicationBuilder app)
    {
        app.UseMultiTenant();
        app.UseMiddleware<TenantGuardMiddleware>();
        return app;
    }
}
