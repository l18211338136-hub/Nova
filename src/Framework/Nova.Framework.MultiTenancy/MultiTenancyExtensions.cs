using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
            .WithClaimStrategy("tenantId")
            .WithDelegateStrategy(async context =>
            {
                if (context is Microsoft.AspNetCore.Http.HttpContext httpContext)
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

                    // 1. 应对 RefreshToken 等带 Header 却可能因为过期失效的原生 WithClaimStrategy
                    if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
                    {
                        var authValue = authHeader.ToString();
                        if (authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            var token = authValue.Substring(7);
                            try
                            {
                                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                                if (handler.CanReadToken(token))
                                {
                                    var jwtToken = handler.ReadJwtToken(token);
                                    var tenantClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenantId");
                                    if (tenantClaim != null && !string.IsNullOrWhiteSpace(tenantClaim.Value))
                                    {
                                        return tenantClaim.Value;
                                    }
                                }
                            }
                            catch { /* 忽略解析错误 */ }
                        }
                    }

                }
                return null;
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
