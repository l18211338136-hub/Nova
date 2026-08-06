using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Framework.Web.Modular;
using Nova.Modules.Identity.Application.Database;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;
using Nova.Modules.Identity.Infrastructure;
using Nova.Framework.Persistence.Interceptors;
using Nova.Framework.Persistence.Extensions;
using Nova.Contracts.TrashBin;
using Nova.Framework.Persistence.TrashBin;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nova.Modules.Identity.Api;

public class IdentityModule : IModule
{
    public string Name => "Identity";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.TryAddSingleton<UtcDateTimeParameterInterceptor>();

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var tenantInfo = sp.GetRequiredService<IMultiTenantContextAccessor>().MultiTenantContext?.TenantInfo as NovaTenantInfo;
            var connectionString = tenantInfo?.ConnectionString
                ?? configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString);
            options.ReplaceService<IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();

            options.AddNovaInterceptors(sp);
        });

        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddScoped<ITrashBinService>(sp => new TrashBinService(sp.GetRequiredService<IdentityDbContext>()));

        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequireUppercase = false; 
        })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapIdentityODataEndpoints();

        endpoints.MapGet("/api/identity/resolve-tenant", async (string account, NovaTenantDbContext tenantDb) =>
        {
            var mappings = await tenantDb.GlobalUserTenantMappings
                .Where(m => m.Account == account)
                .Select(m => m.TenantId)
                .ToListAsync();

            return Results.Ok(new { TenantIds = mappings });
        })
        .WithName("ResolveTenant")
        .WithTags("Auth")
        .WithSummary("解析租户")
        .AllowAnonymous();
    }
}
