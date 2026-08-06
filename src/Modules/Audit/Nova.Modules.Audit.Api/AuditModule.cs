using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Framework.Persistence.Extensions;
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

            options.AddNovaInterceptors(sp);
        });

        services.AddScoped<IAuditDbContext>(sp => sp.GetRequiredService<AuditDbContext>());

        services.AddHostedService<LogProcessorHostedService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
