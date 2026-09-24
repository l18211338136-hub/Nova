using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nova.Framework.MultiTenancy.EntityFrameworkCore;
using Nova.Modules.Mcp.Domain.Entities;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Nova.Framework.Persistence.Extensions;
using System.Threading;
using System.Threading.Tasks;

using Nova.Modules.Mcp.Application.Common.Interfaces;

namespace Nova.Modules.Mcp.Infrastructure.Persistence
{
    public class McpDbContext : DbContext, IMultiTenantDbContext, IMcpDbContext
    {
        public ITenantInfo TenantInfo { get; }
        public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Ignore;
        public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Overwrite;

        public DbSet<McpServer> McpServers { get; set; }
        public DbSet<McpTool> McpTools { get; set; }
        public DbSet<McpKey> McpKeys { get; set; }

        public McpDbContext(ITenantInfo tenantInfo, DbContextOptions<McpDbContext> options)
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
            
            // 自动配置多租户隔离
            builder.ApplyTenantIsolationByDefault();
            
            // 指定独立架构
            builder.HasDefaultSchema("mcp");

            builder.Entity<McpServer>(b =>
            {
                b.ToTable("McpServers");
                b.HasKey(s => s.Id);
                b.Property(s => s.Name).IsRequired().HasMaxLength(100);
                b.Property(s => s.BaseUrl).IsRequired().HasMaxLength(500);
                b.Property(s => s.SwaggerUrl).HasMaxLength(500);
                b.Property(s => s.AuthToken).HasMaxLength(1000);
                
                // 显式声明多租户
                b.IsMultiTenant();
            });

            builder.Entity<McpTool>(b =>
            {
                b.ToTable("McpTools");
                b.HasKey(t => t.Id);
                b.Property(t => t.Name).IsRequired().HasMaxLength(100);
                b.Property(t => t.Description).HasMaxLength(2000);
                b.Property(t => t.HttpMethod).IsRequired().HasMaxLength(20);
                b.Property(t => t.RoutePath).IsRequired().HasMaxLength(500);

                // 将大文本字段用于存储 JSON 结构
                b.Property(t => t.InputSchema).HasColumnType("text");
                b.Property(t => t.ParameterMap).HasColumnType("text");

                // 关联外键
                b.HasOne<McpServer>()
                 .WithMany()
                 .HasForeignKey(t => t.ServerId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.IsMultiTenant();
            });

            builder.Entity<McpKey>(b =>
            {
                b.ToTable("McpKeys");
                b.HasKey(k => k.Id);
                b.Property(k => k.Name).IsRequired().HasMaxLength(100);
                b.Property(k => k.KeyValue).IsRequired().HasMaxLength(1000);
                b.Property(k => k.ExpiresAt).HasColumnType("timestamp with time zone");

                b.IsMultiTenant();
            });


            // 自动应用所有实现了 IFullAuditedEntity/ISoftDelete 的软删除全局过滤
            builder.ApplySoftDeleteQueryFilter();
        }
    }
}
