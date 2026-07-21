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
using Nova.Modules.Identity.Infrastructure;

namespace Nova.Modules.Identity.Api;

public class IdentityModule : IModule
{
    public string Name => "Identity";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add EF Core PostgreSQL with dynamic tenant connection string
        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var tenantInfo = sp.GetRequiredService<IMultiTenantContextAccessor>().MultiTenantContext?.TenantInfo as NovaTenantInfo;
            var connectionString = tenantInfo?.ConnectionString
                ?? configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString);
            options.ReplaceService<IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();
        });

        // Register the IIdentityDbContext to resolve to IdentityDbContext
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());

        // Register ASP.NET Core Identity
        services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequireUppercase = false; // 用户要求密码为 qwe@123!，无大写字母
        })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Endpoints are automatically generated via [ApiEndpoint] characteristics on CQRS commands
        // Custom non-CQRS endpoints can still be added here if needed.
    }
}





