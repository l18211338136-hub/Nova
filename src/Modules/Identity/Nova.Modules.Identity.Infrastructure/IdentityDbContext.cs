using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Modules.Identity.Application.Database;
using Nova.Modules.Identity.Domain;
using Nova.Modules.Identity.Domain.Users;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Menus;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Nova.Framework.Persistence.Extensions;

namespace Nova.Modules.Identity.Infrastructure;

public class IdentityDbContext : IdentityDbContext<User, Role, Guid>, IIdentityDbContext, IMultiTenantDbContext
{
    public ITenantInfo TenantInfo { get; }
    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;

    public DbSet<Menu> Menus { get; set; }

    public DbSet<AuthAuditLog> AuthAuditLogs { get; set; }

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
        
        // Remove unique constraint for User and Role names, uniqueness validation will be handled in code
        builder.Entity<User>(b =>
        {
            var index = b.Metadata.GetIndexes().FirstOrDefault(i => i.GetDatabaseName() == "UserNameIndex");
            if (index != null) b.Metadata.RemoveIndex(index);
            
            b.HasIndex("NormalizedUserName").HasDatabaseName("UserNameIndex").IsUnique(false);
            
            var emailIndex = b.Metadata.GetIndexes().FirstOrDefault(i => i.GetDatabaseName() == "EmailIndex");
            if (emailIndex != null) b.Metadata.RemoveIndex(emailIndex);
            
            b.HasIndex("NormalizedEmail").HasDatabaseName("EmailIndex").IsUnique(false);
        });

        builder.Entity<Role>(b =>
        {
            var index = b.Metadata.GetIndexes().FirstOrDefault(i => i.GetDatabaseName() == "RoleNameIndex");
            if (index != null) b.Metadata.RemoveIndex(index);
            
            b.HasIndex("NormalizedName").HasDatabaseName("RoleNameIndex").IsUnique(false);
        });

        // 菜单配置
        builder.Entity<Menu>(b =>
        {
            b.ToTable("Menus");
            b.HasKey(m => m.Id);
            b.Property(m => m.Name).IsRequired().HasMaxLength(100);
            b.Property(m => m.Path).IsRequired().HasMaxLength(200);
            b.Property(m => m.Component).IsRequired().HasMaxLength(200);
            b.Property(m => m.Icon).HasMaxLength(100);
            
            // 自引用树状结构
            b.HasOne<Menu>()
             .WithMany()
             .HasForeignKey(m => m.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
             
            // 多租户
            b.IsMultiTenant();
        });
        
        // 自动应用所有实现了 IFullAuditedEntity 接口的实体的软删除全局过滤 (IsDeleted == false)
        builder.ApplySoftDeleteQueryFilter();
    }
}
