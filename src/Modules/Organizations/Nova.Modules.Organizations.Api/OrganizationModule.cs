using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Framework.Persistence.Extensions;
using Nova.Framework.Persistence.Interceptors;
using Nova.Framework.Web.Modular;
using Nova.Modules.Organizations.Application.Database;
using Nova.Modules.Organizations.Infrastructure;

namespace Nova.Modules.Organizations.Api;

public class OrganizationModule : IModule
{
    public string Name => "Organization";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<UtcDateTimeParameterInterceptor>();

        services.AddDbContext<OrganizationDbContext>((sp, options) =>
        {
            var tenantInfo = sp.GetRequiredService<IMultiTenantContextAccessor>().MultiTenantContext?.TenantInfo as NovaTenantInfo;
            var connectionString = tenantInfo?.ConnectionString
                ?? configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString);
            options.AddNovaInterceptors(sp);
        });

        services.AddScoped<IOrganizationDbContext>(sp => sp.GetRequiredService<OrganizationDbContext>());
        services.AddScoped<IDbInitializer, OrganizationDbInitializer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOrganizationODataEndpoints();
    }
}
