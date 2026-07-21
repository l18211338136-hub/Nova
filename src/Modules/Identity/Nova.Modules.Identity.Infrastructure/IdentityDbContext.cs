using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Modules.Identity.Application.Database;
using Nova.Modules.Identity.Domain.Users;
using Nova.Modules.Identity.Domain.Roles;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;

namespace Nova.Modules.Identity.Infrastructure;

public class IdentityDbContext : IdentityDbContext<User, Role, Guid>, IIdentityDbContext, IMultiTenantDbContext
{
    public ITenantInfo TenantInfo { get; }
    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;

    public IdentityDbContext(ITenantInfo tenantInfo, DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
        TenantInfo = tenantInfo;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.ApplyTenantIsolationByDefault();
        
        // 显式为 Identity 核心表配置多租户（Finbuckle v7+ 中 IsMultiTenant 是非泛型扩展方法）
        builder.Entity(typeof(User)).IsMultiTenant();
        builder.Entity(typeof(Role)).IsMultiTenant();
        builder.Entity(typeof(IdentityUserClaim<Guid>)).IsMultiTenant();
        builder.Entity(typeof(IdentityUserLogin<Guid>)).IsMultiTenant();
        builder.Entity(typeof(IdentityUserToken<Guid>)).IsMultiTenant();
        builder.Entity(typeof(IdentityUserRole<Guid>)).IsMultiTenant();
        builder.Entity(typeof(IdentityRoleClaim<Guid>)).IsMultiTenant();
        
        builder.HasDefaultSchema("identity");

        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName) && tableName.StartsWith("AspNet"))
            {
                entity.SetTableName(tableName.Substring(6));
            }
        }

    }
}
