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
    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Ignore;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Overwrite;

    public DbSet<Menu> Menus { get; set; }

    public DbSet<AuthAuditLog> AuthAuditLogs { get; set; }

    public DbSet<UserPreference> UserPreferences { get; set; }

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

        // 审计日志配置：显式多租户隔离，确保迁移生成 TenantId 列（阴影属性，与 Menu 一致）。
        // ApplyTenantIsolationByDefault 的反射式 IsMultiTenant<T> 未能为 AuthAuditLog 生成该列，故此处显式注册。
        builder.Entity<AuthAuditLog>(b =>
        {
            b.ToTable("AuthAuditLogs");
            b.HasKey(a => a.Id);
            b.IsMultiTenant();
        });

        // 用户资料扩展字段长度约束
        builder.Entity<User>(b =>
        {
            b.Property(u => u.NickName).HasMaxLength(50);
            b.Property(u => u.AvatarUrl).HasMaxLength(500);
            b.Property(u => u.Bio).HasMaxLength(160);
        });

        // 用户偏好：与 AuthAuditLog 同理，需显式 IsMultiTenant 才能生成 TenantId 列。
        builder.Entity<UserPreference>(b =>
        {
            b.ToTable("UserPreferences");
            b.HasKey(p => p.Id);
            b.Property(p => p.Theme).IsRequired().HasMaxLength(20);
            b.Property(p => p.Font).IsRequired().HasMaxLength(50);
            b.Property(p => p.Language).IsRequired().HasMaxLength(20);
            b.Property(p => p.TimeZone).HasMaxLength(64);
            b.Property(p => p.NotifyType).IsRequired().HasMaxLength(20);
            b.Property(p => p.HiddenSidebarItems).HasMaxLength(2000);
            // 一个用户一行；租户内唯一即可（TenantId 为阴影属性，已由全局过滤器隔离）
            b.HasIndex(p => p.UserId);
            b.IsMultiTenant();
        });

        // 自动应用所有实现了 IFullAuditedEntity 接口的实体的软删除全局过滤 (IsDeleted == false)
        builder.ApplySoftDeleteQueryFilter();
    }
}
