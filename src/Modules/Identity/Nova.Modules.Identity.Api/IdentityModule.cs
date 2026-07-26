using Finbuckle.MultiTenant.Abstractions;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Nova.Framework.MultiTenancy;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Framework.Web.Modular;
using Nova.Modules.Identity.Application.Database;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;
using Nova.Modules.Identity.Domain.Menus;
using Nova.Modules.Identity.Infrastructure;
using Nova.Framework.Persistence.Interceptors;

namespace Nova.Modules.Identity.Api;

public class IdentityModule : IModule
{
    public string Name => "Identity";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Required for CurrentUser to access HttpContext since AddIdentityCore doesn't register it automatically
        services.AddHttpContextAccessor();

        // Add EF Core PostgreSQL with dynamic tenant connection string
        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var tenantInfo = sp.GetRequiredService<IMultiTenantContextAccessor>().MultiTenantContext?.TenantInfo as NovaTenantInfo;
            var connectionString = tenantInfo?.ConnectionString
                ?? configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString);
            options.ReplaceService<IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();

            // Add SaveChanges interceptor
            var interceptor = sp.GetService<AuditableEntitySaveChangesInterceptor>();
            if (interceptor != null)
            {
                options.AddInterceptors(interceptor);
            }
        });

        // Register the IIdentityDbContext to resolve to IdentityDbContext
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());

        // Register ASP.NET Core Identity without Cookie Authentication
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
    }
}





