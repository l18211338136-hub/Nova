using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Finbuckle.MultiTenant.Extensions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nova.Framework.MultiTenancy;

public static class MultiTenancyExtensions
{
    public static IServiceCollection AddNovaMultiTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NovaTenantDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Database=nova_db;Username=postgres;Password=123456";
            options.UseNpgsql(connectionString);
            options.ReplaceService<IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();
        });

        services.AddMultiTenant<NovaTenantInfo>()
            .WithDelegateStrategy(context =>
            {
                if (context is Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
                    {
                        var headerValue = headerTenantId.ToString();
                        if (!string.IsNullOrWhiteSpace(headerValue))
                            return Task.FromResult<string?>(headerValue);
                    }
                        
                    if (httpContext.Request.Query.TryGetValue("tenantId", out var queryTenantId))
                    {
                        var queryValue = queryTenantId.ToString();
                        if (!string.IsNullOrWhiteSpace(queryValue))
                            return Task.FromResult<string?>(queryValue);
                    }
                }
                return Task.FromResult<string?>(null);
            })
            .WithHostStrategy()
            .WithStore<EFCoreStore<NovaTenantDbContext, NovaTenantInfo>>(ServiceLifetime.Scoped); 

        services.AddScoped<ITenantInfo>(sp => 
        {
            var accessor = sp.GetRequiredService<IMultiTenantContextAccessor<NovaTenantInfo>>();
            return accessor.MultiTenantContext?.TenantInfo ?? new NovaTenantInfo { Id = "default", Identifier = "default" };
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
