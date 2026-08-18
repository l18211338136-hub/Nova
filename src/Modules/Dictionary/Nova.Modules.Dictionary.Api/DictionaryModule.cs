using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Framework.Persistence.Extensions;
using Nova.Framework.Web.Modular;
using Nova.Modules.Dictionary.Application.Database;
using Nova.Modules.Dictionary.Application.Dtos;
using Nova.Modules.Dictionary.Domain.DictionaryItems;
using Nova.Modules.Dictionary.Domain.DictionaryTypes;
using Nova.Modules.Dictionary.Infrastructure;
using Nova.Modules.Dictionary.Infrastructure.Persistence;

namespace Nova.Modules.Dictionary.Api;

public class DictionaryModule : IModule
{
    public string Name => "Dictionary";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DictionaryDbContext>((sp, options) =>
        {
            var tenantInfo = sp.GetRequiredService<IMultiTenantContextAccessor>().MultiTenantContext?.TenantInfo as NovaTenantInfo;
            var connectionString = tenantInfo?.ConnectionString
                ?? configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString);
            options.ReplaceService<IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();

            options.AddNovaInterceptors(sp);
        });

        services.AddScoped<IDictionaryDbContext>(sp => sp.GetRequiredService<DictionaryDbContext>());
        services.AddScoped<IDbInitializer, DictionaryDbInitializer>();

        Mapster.TypeAdapterConfig<DictionaryType, DictionaryTypeDto>.NewConfig().Map(dest => dest.SortOrder, src => src.Sort);
        Mapster.TypeAdapterConfig<DictionaryItem, DictionaryItemDto>.NewConfig().Map(dest => dest.SortOrder, src => src.Sort);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDictionaryODataEndpoints();
    }
}
